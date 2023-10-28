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
    private static extern void MovingBlockEntity_ctor(long id, float width, float height, nint addImage);
    [DllImport("NativeLibraryMod")]
    private static extern void MovingBlockEntity_dtor(long id);
    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_Update(long id, MovingBlockEntity* _this, float deltaTime, Vector2* position, nint _base, nint moveH, nint moveV, nint collideCheckSolid);
    [DllImport("NativeLibraryMod")]
    private static extern unsafe void MovingBlockEntity_Render(long id, Vector2 position, Vector2 extend, List<MTexture>* arrowSprites, List<Image>* bodySprites, nint getListCount, nint indexListDrawCentered, nint indexListRender, nint drawRect);
    [DllImport("NativeLibraryMod")]
    private static extern DashCollisionResults MovingBlockEntity_OnDashed(long id, Vector2 direction);

    private static unsafe T* GetPtr<T>(T obj) => (T*)Unsafe.AsPointer(ref obj);

    private unsafe delegate void AddImageDelegate(byte* key, int x, int y, int width, int height, float posX, float posY);
    public unsafe MovingBlockEntity(Vector2 position, float width, float height)
        : base(position, width, height, safe: false)
    {
        MovingBlockEntity_ctor(ID,
            width,
            height,
            Marshal.GetFunctionPointerForDelegate<AddImageDelegate>((key, texX, texY, texWidth, texHeight, posX, posY) => {
                var image = new Image(GFX.Game[Marshal.PtrToStringUTF8((nint)key)].GetSubtexture(texX, texY, texWidth, texHeight)) {
                    Position = new Vector2(posX, posY),
                }.CenterOrigin();
                Add(image);
                BodySprites.Add(image);
            }));
        OnDashCollide = (_, direction) => MovingBlockEntity_OnDashed(ID, direction);
        ArrowSprites = GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow");
        ArrowSprites.Add(GFX.Game["objects/moveBlock/x"]);
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