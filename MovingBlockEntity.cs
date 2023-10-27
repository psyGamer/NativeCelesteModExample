using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.NativeLibraryMod;

[CustomEntity("NativeLibraryMod/MovingBlock")]
public class MovingBlockEntity : Solid {

    private static Color IdleBgFill = Calc.HexToColor("474070");
    private static Color PressedBgFill = Calc.HexToColor("30b335");
    
    private Vector2 Direction = Vector2.Zero;
    private Color FillColor = IdleBgFill;
    private List<MTexture> ArrowSprites = new();
    private List<Image> BodySprites = new();
    
    public MovingBlockEntity(Vector2 position, float width, float height)
        : base(position, width, height, safe: false) {
        OnDashCollide = OnDashed;
        ArrowSprites = GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow");

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

    public override void Update() {
        base.Update();

        if (!CollideCheck<Solid>(Position + Vector2.UnitX * Direction.X)) {
            MoveH(Direction.X);
        } else {
            Direction.X *= -1;
        }
        if (!CollideCheck<Solid>(Position + Vector2.UnitY * Direction.Y)) {
            MoveV(Direction.Y);
        } else {
            Direction.Y *= -1;
        }
        
        FillColor = Color.Lerp(FillColor, Direction != Vector2.Zero ? PressedBgFill : IdleBgFill, 10f * Engine.DeltaTime);
    }
    
    public override void Render() {
        Vector2 position = Position;
        Position += Shake;

        Draw.Rect(X + 3f, Y + 3f, Width - 6f, Height - 6f, FillColor);
        foreach (var body in BodySprites)
            body.Render();
        Draw.Rect(Center.X - 4f, Center.Y - 4f, 8f, 8f, FillColor);
        
        if (Direction != Vector2.Zero)
            ArrowSprites[Calc.Clamp((int) Math.Floor((-(double) Direction.Angle() + Calc.Circle) % Calc.Circle / Calc.Circle * 8.0 + 0.5), 0, 7)].DrawCentered(Center);
        else
            GFX.Game["objects/moveBlock/x"].DrawCentered(Center);

        Position = position;
    }

    private DashCollisionResults OnDashed(Player player, Vector2 direction) {
        Direction += direction;
        return DashCollisionResults.Rebound;
    }
}