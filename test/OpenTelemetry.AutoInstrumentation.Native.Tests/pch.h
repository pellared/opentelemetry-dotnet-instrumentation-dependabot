//
// pch.h
// Header for standard system include files.
//

#ifndef OTEL_CLR_PROFILER_TESTS_PCH_H_
#define OTEL_CLR_PROFILER_TESTS_PCH_H_

#define GTEST_LANG_CXX11 1

#include "gtest/gtest.h"

#include <corhlpr.h>
#include <corprof.h>
#include <metahost.h>
#pragma comment(lib, "mscoree.lib")

#endif