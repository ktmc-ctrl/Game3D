// SengokuSim module: engine-independent simulation layer.
// This header must stay free of Engine / Actor / Gameplay Framework includes.
#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleInterface.h"

SENGOKUSIM_API DECLARE_LOG_CATEGORY_EXTERN(LogSengokuSim, Log, All);

class FSengokuSimModule : public IModuleInterface
{
public:
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
};
