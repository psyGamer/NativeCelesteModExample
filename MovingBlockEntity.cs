using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Celeste.Mod.NativeLibraryMod;

[CustomEntity("NativeLibraryMod/MovingBlock")]
public class MovingBlockEntity : Solid {
    
    private static Color IdleBgFill = Calc.HexToColor("474070");
    private static Color PressedBgFill = Calc.HexToColor("30b335");
    
    private Vector2 Direction = Vector2.Zero;
    private Color FillColor = IdleBgFill;
    private List<MTexture> ArrowSprites = new();
    private List<Image> BodySprites = new();

    private long ID = Calc.Random.NextInt64();

    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_ctor(long id);
    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_dtor(long id);
    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_Update(long id, MovingBlockEntity* _this, Vector2* position, nint _base, nint moveH, nint moveV, nint collideCheckSolid);
    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_Render(long id, Vector2 center, List<MTexture>* arrowSprties, nint getListCount, nint indexListDrawCentered);
    [DllImport("NativeLibraryMod")]
    private static extern unsafe DashCollisionResults MovingBlockEntity_OnDashed(long id, Vector2 direction);

    private static unsafe T* GetPtr<T>(T obj) => (T*)Unsafe.AsPointer(ref obj);
    private static unsafe T* GetPtrRef<T>(ref T obj) => (T*)Unsafe.AsPointer(ref obj);
    private unsafe ref byte GetPin(object obj) => ref *(byte*)*(void**) Unsafe.AsPointer(ref obj);

    // Wrappers to be able to call them from native code.
    private bool CollideCheckSolid(float x, float y) => CollideCheck<Solid>(new Vector2(x, y));

    public unsafe MovingBlockEntity(Vector2 position, float width, float height)
        : base(position, width, height, safe: false)
    {
        MovingBlockEntity_ctor(ID);
        OnDashCollide = (_, direction) => MovingBlockEntity_OnDashed(ID, direction);
        ArrowSprites = GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow");
        ArrowSprites.Add(GFX.Game["objects/moveBlock/x"]);
        //
        // int tilesWidth = (int)(width / 8f), tilesHeight = (int)(height / 8f);
        // for (int x = 0; x < tilesWidth; x++) {
        //     for (int y = 0; y < tilesHeight; y++) {
        //         int hSpriteIdx = x == 0 ? 0 : (x < tilesWidth  - 1 ? 1 : 2);
        //         int vSpriteIdx = y == 0 ? 0 : (y < tilesHeight - 1 ? 1 : 2);
        //
        //         var image = new Image(GFX.Game["objects/moveBlock/base"].GetSubtexture(hSpriteIdx * 8, vSpriteIdx * 8, 8, 8)) {
        //             Position = new Vector2(x * 8f + 4f, y * 8f + 4f),
        //         }.CenterOrigin();
        //         Add(image);
        //         BodySprites.Add(image);
        //     }
        // }
    }
    public MovingBlockEntity(EntityData data, Vector2 offset) 
        : this(data.Position + offset, data.Width, data.Height) { }

    ~MovingBlockEntity() {
        MovingBlockEntity_dtor(ID);
    }
    
    private delegate void MoveDelegate(float move);
    private delegate bool CheckSolidDelegate(float x, float y);
    public override unsafe void Update()
    {
        fixed (Vector2* pPos = &Position) {
            MovingBlockEntity_Update(ID, 
                GetPtr(this), 
                pPos, 
                Marshal.GetFunctionPointerForDelegate(base.Update), 
                Marshal.GetFunctionPointerForDelegate<MoveDelegate>(MoveH), 
                Marshal.GetFunctionPointerForDelegate<MoveDelegate>(MoveV),
                Marshal.GetFunctionPointerForDelegate<CheckSolidDelegate>(CollideCheckSolid));
        }
        
        // base.Update();

        // if (!CollideCheck<Solid>(Position + Vector2.UnitX * Direction.X)) {
        //     MoveH(Direction.X);
        // } else {
        //     Direction.X *= -1;
        // }
        // if (!CollideCheck<Solid>(Position + Vector2.UnitY * Direction.Y)) {
        //     MoveV(Direction.Y);
        // } else {
        //     Direction.Y *= -1;
        // }
        //
        // FillColor = Color.Lerp(FillColor, Direction != Vector2.Zero ? PressedBgFill : IdleBgFill, 10f * Engine.DeltaTime);
    }
    
    private unsafe delegate int ListCountDelegate(List<MTexture>* list);
    private unsafe delegate void ListIndexDrawCenteredDelegate(List<MTexture>* list, int index, float x, float y);
    public override unsafe void Render() {
        fixed (List<MTexture>* pList = &ArrowSprites)
        {
            byte a = 0;
            ref byte pin = ref a;
            MovingBlockEntity_Render(ID,
                Center,
                pList,
                Marshal.GetFunctionPointerForDelegate<ListCountDelegate>(list => list->Count),
                Marshal.GetFunctionPointerForDelegate<ListIndexDrawCenteredDelegate>(
                    (list, index, x, y) => (*list)[index].DrawCentered(new Vector2(x, y))));
            // handles.ForEach(handle => handle.Free());
        }
        // Vector2 position = Position;
        // Position += Shake;
        //
        // Draw.Rect(X + 3f, Y + 3f, Width - 6f, Height - 6f, FillColor);
        // foreach (var body in BodySprites)
        //     body.Render();
        // Draw.Rect(Center.X - 4f, Center.Y - 4f, 8f, 8f, FillColor);
        //
        // if (Direction != Vector2.Zero)
        // ArrowSprites[Calc.Clamp((int) Math.Floor((-(double) Direction.Angle() + Calc.Circle) % Calc.Circle / Calc.Circle * 8.0 + 0.5), 0, 7)].DrawCentered(Center);
        // else
        //     GFX.Game["objects/moveBlock/x"].DrawCentered(Center);
        //
        // Position = position;
    }

    // private DashCollisionResults OnDashed(Player player, Vector2 direction) {
    //     Direction += direction;
    //     return DashCollisionResults.Rebound;
    // }
}