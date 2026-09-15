#include "SengokuSim.h"

#include "Modules/ModuleManager.h"

DEFINE_LOG_CATEGORY(LogSengokuSim);

void FSengokuSimModule::StartupModule()
{
	UE_LOG(LogSengokuSim, Log, TEXT("SengokuSim module started"));
}

void FSengokuSimModule::ShutdownModule()
{
	UE_LOG(LogSengokuSim, Log, TEXT("SengokuSim module shut down"));
}

IMPLEMENT_MODULE(FSengokuSimModule, SengokuSim)
