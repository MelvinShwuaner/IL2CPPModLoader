// manic.js - iOS 26 TXM JIT Support Script for CoreCLR Emulator
// Optimized for CoreCLR using Geode.js's infinite loop mode
// Features:
// 1. Only handles brk #0x69 (JIT memory mapping requests)
// 2. Keeps StikDebug connection alive indefinitely
// 3. Supports JIT memory requests for multiple code blocks

function littleEndianHexStringToNumber(hexStr) {
    const bytes = [];
    for (let i = 0; i < hexStr.length; i += 2) {
        bytes.push(parseInt(hexStr.substr(i, 2), 16));
    }
    let num = 0n;
    for (let i = 4; i >= 0; i--) {
        num = (num << 8n) | BigInt(bytes[i]);
    }
    return num;
}

function numberToLittleEndianHexString(num) {
    const bytes = [];
    for (let i = 0; i < 5; i++) {
        bytes.push(Number(num & 0xFFn));
        num >>= 8n;
    }
    while (bytes.length < 8) {
        bytes.push(0);
    }
    return bytes.map(b => b.toString(16).padStart(2, '0')).join('');
}

function littleEndianHexToU32(hexStr) {
    return parseInt(hexStr.match(/../g).reverse().join(''), 16);
}

function extractBrkImmediate(u32) {
    return (u32 >> 5) & 0xFFFF;
}

// Check if the instruction is a BRK instruction
// BRK instruction format: 0xD4200000 | (imm16 << 5)
// The upper 16 bits should be 0xD420
function isBrkInstruction(u32) {
    return (u32 >>> 16) === 0xD420;
}

// Format size into a human-readable format
function formatSize(size) {
    if (size >= 1024 * 1024) {
        return `${(size / (1024 * 1024)).toFixed(2)} MB`;
    } else if (size >= 1024) {
        return `${(size / 1024).toFixed(2)} KB`;
    }
    return `${size} bytes`;
}

function nowTimestamp() {
    const epochMs = Date.now();
    return {
        epochMs,
        iso: new Date(epochMs).toISOString()
    };
}

function pushTimelineEvent(timeline, event) {
    timeline.push(event);
    // Keep bounded memory while still preserving recent sequencing.
    if (timeline.length > 4096) {
        timeline.shift();
    }
}

log(`[CoreCLR] ========================================`);
log(`[CoreCLR] CoreCLR iOS 26 TXM JIT Support Script`);
log(`[CoreCLR] ========================================`);

let pid = get_pid();
log(`[CoreCLR] pid = ${pid}`);
let attachResponse = send_command(`vAttach;${pid.toString(16)}`);
log(`[CoreCLR] attach_response = ${attachResponse}`);

let validBreakpoints = 0;
let totalBreakpoints = 0;
let totalMemoryMapped = 0n;

// Track processed memory areas for debugging.
let mappedRegions = [];
let requestTimeline = [];
let resumeTimeline = [];

// Infinite Loop - StikDebug Must Stay Connected in TXM Mode
// The CoreCLR emulator dynamically creates multiple code blocks, each of which needs to be marked as executable by StikDebug.
log(`[CoreCLR] Starting infinite loop - StikDebug will stay connected`);
log(`[CoreCLR] Waiting for JIT memory requests (brk #0x69)...`);

while (true) {
    totalBreakpoints++;
    const waitTs = nowTimestamp();
    log(`[CoreCLR] Waiting for stop #${totalBreakpoints} at ${waitTs.iso} (${waitTs.epochMs})`);
    let brkResponse = send_command(`c`);
    const recvTs = nowTimestamp();
    log(`[CoreCLR] Received stop #${totalBreakpoints} at ${recvTs.iso} (${recvTs.epochMs}), wait_ms=${recvTs.epochMs - waitTs.epochMs}`);
    
    // Check Exception Types (metype)
    // metype:6 = EXC_BREAKPOINT
    let metypeMatch = /metype:(\d+)/.exec(brkResponse);
    let metype = metypeMatch ? parseInt(metypeMatch[1]) : -1;
    
    // If it's not a breakpoint exception, forward original signal like universal.js.
    if (metype !== 6 && metype !== -1) {
        let tidMatchNB = /T[0-9a-f]+thread:(?<tid>[0-9a-f]+);/.exec(brkResponse);
        let tidNB = tidMatchNB ? tidMatchNB.groups['tid'] : null;
        let sigMatchNB = /^T(?<sig>[a-z0-9;]{2})/.exec(brkResponse);
        let sigNB = sigMatchNB ? sigMatchNB.groups['sig'] : null;
        if (tidNB && sigNB) {
            let passResponse = send_command(`vCont;S${sigNB}:${tidNB}`);
            log(`[CoreCLR] Non-breakpoint exception metype=${metype}, forwarded signal 0x${sigNB}, response=${passResponse}`);
        } else {
            log(`[CoreCLR] Non-breakpoint exception metype=${metype}, missing tid/sig, continuing`);
        }
        continue;
    }
    
    let tidMatch = /T[0-9a-f]+thread:(?<tid>[0-9a-f]+);/.exec(brkResponse);
    let tid = tidMatch ? tidMatch.groups['tid'] : null;
    let pcMatch = /20:(?<reg>[0-9a-f]{16});/.exec(brkResponse);
    let pc = pcMatch ? pcMatch.groups['reg'] : null;
    let x0Match = /00:(?<reg>[0-9a-f]{16});/.exec(brkResponse);
    let x0 = x0Match ? x0Match.groups['reg'] : null;
    let x1Match = /01:(?<reg>[0-9a-f]{16});/.exec(brkResponse);
    let x1 = x1Match ? x1Match.groups['reg'] : null;
    
    if (!tid || !pc || !x0) {
        log(`[CoreCLR] Failed to extract registers, continuing...`);
        let sigMatchBad = /^T(?<sig>[a-z0-9;]{2})/.exec(brkResponse);
        let sigBad = sigMatchBad ? sigMatchBad.groups['sig'] : null;
        if (tid && sigBad) {
            let passResponse = send_command(`vCont;S${sigBad}:${tid}`);
            log(`[CoreCLR] Forwarded signal for malformed stop: 0x${sigBad}, response=${passResponse}`);
        }
        continue;
    }

    const pcNum = littleEndianHexStringToNumber(pc);
    const x0Num = littleEndianHexStringToNumber(x0);
    const x1Num = x1 ? littleEndianHexStringToNumber(x1) : 0n;
    
    let instructionResponse = send_command(`m${pcNum.toString(16)},4`);
    let instrU32 = littleEndianHexToU32(instructionResponse);
    
    // Check if it's a BRK instruction.
    if (!isBrkInstruction(instrU32)) {
        // Not a BRK instruction: forward original signal and continue.
        let sigMatch = /^T(?<sig>[a-z0-9;]{2})/.exec(brkResponse);
        let sig = sigMatch ? sigMatch.groups['sig'] : null;
        if (sig) {
            let passResponse = send_command(`vCont;S${sig}:${tid}`);
            log(`[CoreCLR] Not a BRK at PC=0x${pcNum.toString(16)}, forwarded signal 0x${sig}, response=${passResponse}`);
        } else {
            log(`[CoreCLR] Not a BRK at PC=0x${pcNum.toString(16)}, no signal parsed`);
        }
        continue;
    }
    
    let brkImmediate = extractBrkImmediate(instrU32);
    
    // Only handle brk #0x69 (JIT memory mapping request).
    if (brkImmediate === 0x69) {
        validBreakpoints++;
        
        let jitPageAddress = x0Num;
        // If x1 is 0, use the default size of 64KB (0x10000).
        // CoreCLR's CodeBlock typically requests large memory chunks (tens of MB for CPU JIT)
        // but also asks for smaller ones (like 4KB/16KB for SpinLock and Shader JIT).
        let size = x1Num > 0n ? x1Num : 0x10000n;
        
        log(`[CoreCLR] ----------------------------------------`);
        log(`[CoreCLR] JIT Request #${validBreakpoints}`);
        log(`[CoreCLR]   Address: 0x${jitPageAddress.toString(16)}`);
        log(`[CoreCLR]   Size: 0x${size.toString(16)} (${formatSize(Number(size))})`);
        const requestTs = nowTimestamp();
        pushTimelineEvent(requestTimeline, {
            index: validBreakpoints,
            tid: tid,
            pc: `0x${pcNum.toString(16)}`,
            address: `0x${jitPageAddress.toString(16)}`,
            size: Number(size),
            epoch_ms: requestTs.epochMs,
            iso: requestTs.iso
        });
        log(`[CoreCLR]   Request timestamp: ${requestTs.iso} (${requestTs.epochMs})`);
        
        // Call prepare_memory_region to mark the memory as executable.
        let prepareJITPageResponse = prepare_memory_region(Number(jitPageAddress), Number(size));
        log(`[CoreCLR]   prepare_memory_region result: ${prepareJITPageResponse}`);
        
        // Log statistics
        totalMemoryMapped += size;
        mappedRegions.push({
            address: `0x${jitPageAddress.toString(16)}`,
            size: Number(size),
            index: validBreakpoints
        });
        
        log(`[CoreCLR]   Total JIT memory mapped: ${formatSize(Number(totalMemoryMapped))}`);
        
        // Set PC+4 to continue program execution
        let pcPlus4 = numberToLittleEndianHexString(pcNum + 4n);
        send_command(`P20=${pcPlus4};thread:${tid};`);
        const resumeTs = nowTimestamp();
        pushTimelineEvent(resumeTimeline, {
            reason: "brk_0x69",
            tid: tid,
            old_pc: `0x${pcNum.toString(16)}`,
            new_pc: `0x${(pcNum + 4n).toString(16)}`,
            epoch_ms: resumeTs.epochMs,
            iso: resumeTs.iso
        });
        
        log(`[CoreCLR]   Resumed execution at PC=0x${(pcNum + 4n).toString(16)}`);
        log(`[CoreCLR]   Resume timestamp: ${resumeTs.iso} (${resumeTs.epochMs})`);
        log(`[CoreCLR] ----------------------------------------`);
        
    } else if (brkImmediate === 0x70) {
        // brk #0x70 - Memory patching (copy from src to dest)
        // x0 = destination address
        // x1 = source address
        // x2 = size in bytes
        validBreakpoints++;

        let x2Match = /02:(?<reg>[0-9a-f]{16});/.exec(brkResponse);
        let x2 = x2Match ? x2Match.groups['reg'] : null;

        if (!x1 || !x2) {
            log(`[CoreCLR] brk #0x70: Missing x1 or x2 for memory patching, skipping`);
            let pcPlus4 = numberToLittleEndianHexString(pcNum + 4n);
            send_command(`P20=${pcPlus4};thread:${tid};`);
            continue;
        }

        let destAddr = x0Num;
        let srcAddr = x1Num;
        let size = littleEndianHexStringToNumber(x2);

        log(`[CoreCLR] ----------------------------------------`);
        log(`[CoreCLR] Memory Patch Request #${validBreakpoints}`);
        log(`[CoreCLR]   Dest: 0x${destAddr.toString(16)}`);
        log(`[CoreCLR]   Src:  0x${srcAddr.toString(16)}`);
        log(`[CoreCLR]   Size: 0x${size.toString(16)} (${formatSize(Number(size))})`);
        const requestTs = nowTimestamp();
        pushTimelineEvent(requestTimeline, {
            type: "patch",
            index: validBreakpoints,
            tid: tid,
            pc: `0x${pcNum.toString(16)}`,
            dest: `0x${destAddr.toString(16)}`,
            src: `0x${srcAddr.toString(16)}`,
            size: Number(size),
            epoch_ms: requestTs.epochMs,
            iso: requestTs.iso
        });

        // Soft limit to prevent freezing (4 MB max like Geode.js)
        if (size > 0x400000n) {
            log(`[CoreCLR]   Size too large, skipping`);
            let pcPlus4 = numberToLittleEndianHexString(pcNum + 4n);
            send_command(`P20=${pcPlus4};thread:${tid};`);
            continue;
        }

        try {
            // Read from source and write to destination in chunks
            const CHUNK_SIZE = 0x4000n; // 16 KB chunks
            for (let i = 0n; i < size; i += CHUNK_SIZE) {
                let chunkSize = i + CHUNK_SIZE <= size ? CHUNK_SIZE : size - i;
                let readAddr = srcAddr + i;
                let writeAddr = destAddr + i;

                // Read from source
                let readRes = send_command(`m${readAddr.toString(16)},${chunkSize.toString(16)}`);
                if (readRes && readRes.length > 0) {
                    // Write to destination
                    let writeResponse = send_command(`M${writeAddr.toString(16)},${chunkSize.toString(16)}:${readRes}`);
                    if (writeResponse !== "OK") {
                        log(`[CoreCLR]   Write failed at offset 0x${i.toString(16)}`);
                        break;
                    }
                } else {
                    log(`[CoreCLR]   Read failed at offset 0x${i.toString(16)}`);
                    break;
                }

                // Progress logging for large patches
                if (size > CHUNK_SIZE && Number(i / CHUNK_SIZE) % 10 === 0) {
                    log(`[CoreCLR]   Progress: 0x${i.toString(16)}/0x${size.toString(16)}`);
                }
            }
            log(`[CoreCLR]   Memory patch completed!`);
        } catch (e) {
            log(`[CoreCLR]   Memory patch failed: ${e}`);
        }

        // Resume execution at PC+4
        let pcPlus4 = numberToLittleEndianHexString(pcNum + 4n);
        send_command(`P20=${pcPlus4};thread:${tid};`);
        const resumeTs = nowTimestamp();
        pushTimelineEvent(resumeTimeline, {
            reason: "brk_0x70",
            tid: tid,
            old_pc: `0x${pcNum.toString(16)}`,
            new_pc: `0x${(pcNum + 4n).toString(16)}`,
            epoch_ms: resumeTs.epochMs,
            iso: resumeTs.iso
        });
        log(`[CoreCLR]   Resumed at PC=0x${(pcNum + 4n).toString(16)}`);
        log(`[CoreCLR] ----------------------------------------`);

    } else if (brkImmediate === 0x71) {
        // brk #0x71 - Detach request
        log(`[CoreCLR] Detach request (brk #0x71), skipping to PC+4...`);
        let pcPlus4 = numberToLittleEndianHexString(pcNum + 4n);
        send_command(`P20=${pcPlus4};thread:${tid};`);
        const resumeTs = nowTimestamp();
        pushTimelineEvent(resumeTimeline, {
            reason: "brk_0x71",
            tid: tid,
            old_pc: `0x${pcNum.toString(16)}`,
            new_pc: `0x${(pcNum + 4n).toString(16)}`,
            epoch_ms: resumeTs.epochMs,
            iso: resumeTs.iso
        });

    } else {
        // Other BRK instructions, skip PC+4 and continue execution.
        log(`[CoreCLR] Unknown brk #0x${brkImmediate.toString(16)} at PC=0x${pcNum.toString(16)}, skipping...`);
        let pcPlus4 = numberToLittleEndianHexString(pcNum + 4n);
        send_command(`P20=${pcPlus4};thread:${tid};`);
        const resumeTs = nowTimestamp();
        pushTimelineEvent(resumeTimeline, {
            reason: `brk_unknown_0x${brkImmediate.toString(16)}`,
            tid: tid,
            old_pc: `0x${pcNum.toString(16)}`,
            new_pc: `0x${(pcNum + 4n).toString(16)}`,
            epoch_ms: resumeTs.epochMs,
            iso: resumeTs.iso
        });
        log(`[CoreCLR] Resume timestamp: ${resumeTs.iso} (${resumeTs.epochMs})`);
    }
}

// This line of code will never run because it's an infinite loop
// log(`[CoreCLR] Script ended`);
