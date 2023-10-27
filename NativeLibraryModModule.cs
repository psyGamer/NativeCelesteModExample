using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.NativeLibraryMod {
    public class NativeLibraryModModule : EverestModule {
        public static NativeLibraryModModule Instance { get; private set; }

        public override Type SettingsType => typeof(NativeLibraryModModuleSettings);
        public static NativeLibraryModModuleSettings Settings => (NativeLibraryModModuleSettings) Instance._Settings;

        public override Type SessionType => typeof(NativeLibraryModModuleSession);
        public static NativeLibraryModModuleSession Session => (NativeLibraryModModuleSession) Instance._Session;

        public NativeLibraryModModule() {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(nameof(NativeLibraryModModule), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(NativeLibraryModModule), LogLevel.Info);
#endif
        }

        public override void Load() {
            // TODO: apply any hooks that should always be active
        }

        public override void Unload() {
            // TODO: unapply any hooks applied in Load()
        }
    }
}