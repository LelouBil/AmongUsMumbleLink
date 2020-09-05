

#pragma once
#include <string>

#define EXPORT extern "C" __declspec(dllexport)

using namespace std;


EXPORT void init_mumble();

EXPORT void update_mumble(float x, float y, float z,int dir, const char* name, const char* context);


