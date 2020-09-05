// MumbleLink.cpp : Définit les fonctions de la bibliothèque statique.
//

#include "pch.h"

#include "MumbleLink.h"
#include <iostream>
#include <stdio.h>
#include <windows.h>



struct linked_mem {
	UINT32	ui_version;
	DWORD	ui_tick;
	float	f_avatar_position[3];
	float	f_avatar_front[3];
	float	f_avatar_top[3];
	wchar_t	name[256];
	float	f_camera_position[3];
	float	f_camera_front[3];
	float	f_camera_top[3];
	wchar_t	identity[256];
	UINT32	context_len;
	unsigned char context[256];
	wchar_t description[2048];
};

linked_mem* lm = nullptr;


void init_mumble() {
	auto h_map_object = OpenFileMappingW(FILE_MAP_ALL_ACCESS, FALSE, L"MumbleLink");
	if (h_map_object == nullptr)
		return;

	lm = static_cast<linked_mem*>(MapViewOfFile(h_map_object, FILE_MAP_ALL_ACCESS, 0, 0, sizeof(linked_mem)));
	if (lm == nullptr) {
		CloseHandle(h_map_object);
		h_map_object = nullptr;
	}
}

std::wstring s2_ws(const std::string& s) {
	const auto s_lenght = static_cast<int>(s.length()) + 1;
	const auto len = MultiByteToWideChar(CP_ACP, 0, s.c_str(), s_lenght, nullptr, 0);
	std::wstring r(len, L'\0');
	MultiByteToWideChar(CP_ACP, 0, s.c_str(), s_lenght, &r[0], len);
	r.resize(r.size() - 1);
	return r;
}

void update_mumble(const float x, const float y, const float z, const int dir, const char* name, const char* context) {
	if (!lm)
		return;

	if (lm->ui_version != 2) {
		wcsncpy_s(lm->name, L"AmongUsLink", 256);
		wcsncpy_s(lm->description, L"AmongUsLink adds positional audio to Among Us", 2048);
		lm->ui_version = 2;
	}
	lm->ui_tick++;

	// Left handed coordinate system.
	// X positive towards "right".
	// Y positive towards "up".
	// Z positive towards "front".
	//
	// 1 unit = 1 meter

	// Unit vector pointing out of the avatar's eyes aka "At"-vector. 
	lm->f_avatar_front[0] = 0.0f;
	lm->f_avatar_front[1] = 0.0f; //todo -y
	lm->f_avatar_front[2] = 1.0f;

	// Unit vector pointing out of the top of the avatar's head aka "Up"-vector (here Top points straight up).
	lm->f_avatar_top[0] = 0.0f;
	lm->f_avatar_top[1] = 1.0f; //todo +z 
	lm->f_avatar_top[2] = 0.0f;

	// Position of the avatar (here standing slightly off the origin)
	lm->f_avatar_position[0] = x;
	lm->f_avatar_position[1] = y;
	lm->f_avatar_position[2] = z;

	// Same as avatar but for the camera.
	lm->f_camera_position[0] = x;
	lm->f_camera_position[1] = y + 1.0f;
	lm->f_camera_position[2] = z;

	lm->f_camera_front[0] = 0.0f;
	lm->f_camera_front[1] = -1.0f; //todo -y
	lm->f_camera_front[2] = 0.0f;

	lm->f_camera_top[0] = 0.0f;
	lm->f_camera_top[1] = 0.0f; //todo +z
	lm->f_camera_top[2] = 1.0f;

	//todo try cam on top & try player eye up

	const auto wn = s2_ws(name);
	const auto wc = s2_ws(context);

	const auto c_len = wc.length();

	// Identifier which uniquely identifies a certain player in a context (e.g. the in game name).
	wcsncpy_s(lm->identity, wn.c_str(), 256);
	// Context should be equal for players which should be able to hear each other positional and
	// differ for those who shouldn't (e.g. it could contain the server+port and team)
	memcpy(lm->context, wc.c_str(), c_len);
	lm->context_len = c_len;
}


