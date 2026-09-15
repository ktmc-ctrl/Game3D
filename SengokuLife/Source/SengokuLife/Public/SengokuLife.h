// SengokuLife module: game / Unreal world layer. May depend on SengokuSim.
#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleInterface.h"

SENGOKULIFE_API DECLARE_LOG_CATEGORY_EXTERN(LogSengokuLife, Log, All);

class FSengokuLifeModule : public IModuleInterface
{
public:
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
};
