using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.NativeLibraryMod;

#pragma warning disable CS8500
[CustomEntity("NativeLibraryMod/MovingBlock")]
public class MovingBlockEntity : Solid {
    
    private List<MTexture> ArrowSprites;
    private List<Image> BodySprites = new();

    private readonly long ID = Calc.Random.NextInt64();

    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_ctor(long id, 
        MovingBlockEntity* _this,
        float width, float height, 
        delegate* unmanaged[Cdecl]<MovingBlockEntity*, byte*, int, int, int, int, float, float, void> addImage);
    [DllImport("NativeLibraryMod")]
    private static extern void MovingBlockEntity_dtor(long id);
    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_Update(long id, 
        MovingBlockEntity* _this,
        float deltaTime, Vector2* position, 
        delegate* unmanaged[Cdecl]<MovingBlockEntity*, void> _base, 
        delegate* unmanaged[Cdecl]<MovingBlockEntity*, float, void> moveH, 
        delegate* unmanaged[Cdecl]<MovingBlockEntity*, float, void> moveV, 
        delegate* unmanaged[Cdecl]<MovingBlockEntity*, float, float, int> collideCheckSolid);
    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_Render(long id,
        Vector2 position, Vector2 extend,
        List<MTexture>* arrowSprites, List<Image>* bodySprites, 
        delegate* unmanaged[Cdecl]<List<Image>*, int> getListCount,
        delegate* unmanaged[Cdecl]<List<MTexture>*, int, float, float, void> indexListDrawCentered,
        delegate* unmanaged[Cdecl]<List<Image>*, int, void> indexListRender, 
        delegate* unmanaged[Cdecl]<float, float, float, float, Color, void> drawRect);
    [DllImport("NativeLibraryMod")]
    private static extern DashCollisionResults MovingBlockEntity_OnDashed(long id, Vector2 direction);

    // Wrappers for native functions to call
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void AddImage(MovingBlockEntity* _this, byte* key, int x, int y, int width, int height, float posX, float posY) {
        var image = new Image(GFX.Game[Marshal.PtrToStringUTF8((nint)key)].GetSubtexture(x, y, width, height)) {
            Position = new Vector2(posX, posY),
        }.CenterOrigin();
        _this->Add(image);
        _this->BodySprites.Add(image);
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void BlockMoveH(MovingBlockEntity* _this, float moveH) => _this->MoveH(moveH);
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void BlockMoveV(MovingBlockEntity* _this, float moveV) => _this->MoveV(moveV);
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe int CollideCheckSolid(MovingBlockEntity* _this, float x, float y) => _this->CollideCheck<Solid>(new Vector2(x, y)) ? 1 : 0;
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe int GetListCount(List<Image>* _this) => _this->Count;
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void ListIndexDrawCentered(List<MTexture>* list, int index, float x, float y) => (*list)[index].DrawCentered(new Vector2(x, y));
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void ListIndexRender(List<Image>* list, int index) => (*list)[index].Render();
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void DrawRect(float x, float y, float width, float height, Color color) => Draw.Rect(x, y, width, height, color);
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void BaseUpdate(MovingBlockEntity* _this) => _this->base_Update();

    private void base_Update() => base.Update();

    private unsafe MovingBlockEntity(Vector2 position, float width, float height) : base(position, width, height, safe: false) {
        var self = this;
        MovingBlockEntity_ctor(ID, &self, Collider.Width, Collider.Height, &AddImage);    

        OnDashCollide = (_, direction) => MovingBlockEntity_OnDashed(ID, direction);
        ArrowSprites = GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow");
        ArrowSprites.Add(GFX.Game["objects/moveBlock/x"]);
    }
    public MovingBlockEntity(EntityData data, Vector2 offset) 
        : this(data.Position + offset, data.Width, data.Height) { }

    ~MovingBlockEntity() {
        MovingBlockEntity_dtor(ID);
    }
    
    public override unsafe void Update() {
        fixed (Vector2* pPos = &Position) {
            var self = this;
            MovingBlockEntity_Update(ID, &self, Engine.DeltaTime, pPos, 
                &BaseUpdate, &BlockMoveH, &BlockMoveV, &CollideCheckSolid);
        }
    }

    public override unsafe void Render() {
        fixed (List<MTexture>* pArrow = &ArrowSprites) {
            fixed (List<Image>* pBody = &BodySprites) {
                MovingBlockEntity_Render(ID, Position, new Vector2(Width, Height), pArrow, pBody,
                    &GetListCount, &ListIndexDrawCentered, &ListIndexRender, &DrawRect);
            }
        }
    }
}