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
    
    private List<MTexture> ArrowSprites = new();
    private List<Image> BodySprites = new();

    private long ID = Calc.Random.NextInt64();

    [DllImport("NativeLibraryMod")]
    private static extern void MovingBlockEntity_ctor(long id);
    [DllImport("NativeLibraryMod")]
    private static extern void MovingBlockEntity_dtor(long id);
    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_Update(long id, MovingBlockEntity* _this, float deltaTime, Vector2* position, nint _base, nint moveH, nint moveV, nint collideCheckSolid);
    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_Render(long id, Vector2 position, Vector2 extend, List<MTexture>* arrowSprties, List<Image>* bodySprties, nint getListCount, nint indexListDrawCentered, nint indexListRender, nint drawRect);
    [DllImport("NativeLibraryMod")]
    private static extern DashCollisionResults MovingBlockEntity_OnDashed(long id, Vector2 direction);

    private static unsafe T* GetPtr<T>(T obj) => (T*)Unsafe.AsPointer(ref obj);

    public MovingBlockEntity(Vector2 position, float width, float height)
        : base(position, width, height, safe: false)
    {
        MovingBlockEntity_ctor(ID);
        OnDashCollide = (_, direction) => MovingBlockEntity_OnDashed(ID, direction);
        ArrowSprites = GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow");
        ArrowSprites.Add(GFX.Game["objects/moveBlock/x"]);
        
        int tilesWidth = (int)(width / 8f), tilesHeight = (int)(height / 8f);
        for (int x = 0; x < tilesWidth; x++) {
            for (int y = 0; y < tilesHeight; y++) {
                int hSpriteIdx = x == 0 ? 0 : (x < tilesWidth  - 1 ? 1 : 2);
                int vSpriteIdx = y == 0 ? 0 : (y < tilesHeight - 1 ? 1 : 2);
        
                var image = new Image(GFX.Game["objects/moveBlock/base"].GetSubtexture(hSpriteIdx * 8, vSpriteIdx * 8, 8, 8)) {
                    Position = new Vector2(x * 8f + 4f, y * 8f + 4f),
                }.CenterOrigin();
                Add(image);
                BodySprites.Add(image);
            }
        }
    }
    public MovingBlockEntity(EntityData data, Vector2 offset) 
        : this(data.Position + offset, data.Width, data.Height) { }

    ~MovingBlockEntity() {
        MovingBlockEntity_dtor(ID);
    }
    
    private delegate void MoveDelegate(float move);
    private delegate bool CheckSolidDelegate(float x, float y);
    public override unsafe void Update() {
        fixed (Vector2* pPos = &Position) {
            MovingBlockEntity_Update(ID, 
                GetPtr(this), 
                Engine.DeltaTime,
                pPos, 
                Marshal.GetFunctionPointerForDelegate(base.Update), 
                Marshal.GetFunctionPointerForDelegate<MoveDelegate>(MoveH), 
                Marshal.GetFunctionPointerForDelegate<MoveDelegate>(MoveV),
                Marshal.GetFunctionPointerForDelegate<CheckSolidDelegate>((x, y) => CollideCheck<Solid>(new Vector2(x, y))));
        }
    }
    
    private unsafe delegate int ListCountDelegate(List<MTexture>* list);
    private unsafe delegate void ListIndexDrawCenteredDelegate(List<MTexture>* list, int index, float x, float y);
    private unsafe delegate void ListIndexRenderDelegate(List<Image>* list, int index);
    private delegate void DrawRectDelegate(float x, float y, float width, float height, Color color);
    public override unsafe void Render() {
        fixed (List<MTexture>* pArrow = &ArrowSprites) {
            fixed (List<Image>* pBody = &BodySprites) {
                MovingBlockEntity_Render(ID,
                    Position,
                    new Vector2(Width, Height),
                    pArrow,
                    pBody,
                    Marshal.GetFunctionPointerForDelegate<ListCountDelegate>(list => list->Count),
                    Marshal.GetFunctionPointerForDelegate<ListIndexDrawCenteredDelegate>(
                        (list, index, x, y) => (*list)[index].DrawCentered(new Vector2(x, y))),
                    Marshal.GetFunctionPointerForDelegate<ListIndexRenderDelegate>((list, index) => (*list)[index].Render()),
                    Marshal.GetFunctionPointerForDelegate<DrawRectDelegate>(Draw.Rect));
            }
        }
    }
}