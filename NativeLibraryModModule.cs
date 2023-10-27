using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.NativeLibraryMod;

public class NativeLibraryModModule : EverestModule {
    public static NativeLibraryModModule Instance { get; private set; }

    public NativeLibraryModModule() {
        Instance = this;
    }

    public override void Load() {
        // TODO: apply any hooks that should always be active
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
    }
}