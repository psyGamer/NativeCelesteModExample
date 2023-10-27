const std = @import("std");

const EntityID = u64;

const Color = struct {
    r: u8,
    g: u8,
    b: u8,

    pub fn fromHex(hex: []const u8) Color {
        var color: Color = undefined;
        std.fmt.hexToBytes(std.mem.asBytes(&color), hex);
        return color;
    }
};
const Vec2 = extern struct {
    x: f32,
    y: f32,

    pub fn add(self: Vec2, other: Vec2) Vec2 {
        return .{ .x = self.x + other.x, .y = self.y + other.y };
    }
};

const DashCollisionResult = enum(u8) { rebound, normal_collision, normal_override, bounce, ignore };

pub const MovingBlockEntity = struct {
    const Self = @This();
    pub const PfnUpdate = *const fn (this: *anyopaque) void;
    pub const PfnRender = *const fn (this: *anyopaque) void;
    pub const PfnMoveH = *const fn (this: *anyopaque, move_h: f32) void;
    pub const PfnMoveV = *const fn (this: *anyopaque, move_v: f32) void;
    pub const PfnCollideCheckSolid = *const fn (this: *anyopaque, x: f32, y: f32) bool;

    const idle_bg_fill = Color.fromHex("474070");
    const moving_bg_fill = Color.fromHex("30b335");

    var entities: std.AutoHashMapUnmanaged(EntityID, Self) = .{};

    direction: Vec2 = .{ .x = 0, .y = 0 },
    fill_color: Color = .{ .r = 0, .g = 0, .b = 0 },

    fn init() Self {
        return .{};
    }
    fn deinit(self: Self) void {
        _ = self;
    }

    fn update(self: *Self, this: *anyopaque, position: *Vec2, base: PfnUpdate, move_h: PfnMoveH, move_v: PfnMoveV, collide_check_solid: PfnCollideCheckSolid) void {
        base(this);

        if (!collide_check_solid(this, position.x + self.direction.x, position.y)) {
            move_h(this, self.direction.x);
        } else {
            self.direction.x *= -1;
        }
        if (!collide_check_solid(this, position.x, position.y + self.direction.y)) {
            move_v(this, self.direction.y);
        } else {
            self.direction.y *= -1;
        }
    }
    fn render(self: *Self, this: *anyopaque, base: PfnUpdate) void {
        _ = base;
        _ = this;
        _ = self;
    }
    fn on_dashed(self: *Self, direction: Vec2) DashCollisionResult {
        self.direction.x += direction.x;
        self.direction.y += direction.y;
        std.log.info("Direction: {}", .{self.direction});
        return .rebound;
    }
};

pub export fn MovingBlockEntity_ctor(id: EntityID) void {
    MovingBlockEntity.entities.put(std.heap.page_allocator, id, MovingBlockEntity.init()) catch @panic("OOM");
}
pub export fn MovingBlockEntity_dtor(id: EntityID) void {
    MovingBlockEntity.entities.getPtr(id).?.deinit();
    _ = MovingBlockEntity.entities.remove(id);
}
pub export fn MovingBlockEntity_Update(
    id: EntityID,
    this: *anyopaque,
    position: *Vec2,
    base: MovingBlockEntity.PfnUpdate,
    move_h: MovingBlockEntity.PfnMoveH,
    move_v: MovingBlockEntity.PfnMoveV,
    collide_check_solid: MovingBlockEntity.PfnCollideCheckSolid,
) void {
    MovingBlockEntity.entities.getPtr(id).?.update(this, position, base, move_h, move_v, collide_check_solid);
}
pub export fn MovingBlockEntity_Render(id: EntityID, this: *anyopaque, base: MovingBlockEntity.PfnUpdate) void {
    MovingBlockEntity.entities.getPtr(id).?.render(this, base);
}
pub export fn MovingBlockEntity_OnDashed(id: EntityID, direction: Vec2) DashCollisionResult {
    return MovingBlockEntity.entities.getPtr(id).?.on_dashed(direction);
}
