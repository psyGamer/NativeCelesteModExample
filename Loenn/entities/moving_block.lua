local fakeTilesHelper = require("helpers.fake_tiles")

return {
    name = "NativeLibraryMod/MovingBlock",
    depth = 8995,
    minimumSize = {16, 16},
    placements = {
        name = "NativeLibraryMod/MovingBlock",
        data = {
            width = 8,
            height = 8,
        },
    },
    color = {1.0, 0.0, 0.0}
--     sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", "blendin"),
--     fieldInformation = fakeTilesHelper.getFieldInformation("tiletype"),
}