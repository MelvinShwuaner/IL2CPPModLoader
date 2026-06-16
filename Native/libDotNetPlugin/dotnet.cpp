static bool Hosted = false;
extern "C" {
int IsHosting() {
    return Hosted;
}
}