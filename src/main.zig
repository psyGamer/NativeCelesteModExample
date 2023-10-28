const std = @import("std");

const EntityID = u64;

const Color = packed struct(u32) {
    r: u8 = 0,
    g: u8 = 0,
    b: u8 = 0,
    a: u8 = 255,

    pub fn lerp(a: Color, b: Color, t: f32) Color {
        return .{
            .r = @intFromFloat(std.math.lerp(@as(f32, @floatFromInt(a.r)), @as(f32, @floatFromInt(b.r)), t)),
            .g = @intFromFloat(std.math.lerp(@as(f32, @floatFromInt(a.g)), @as(f32, @floatFromInt(b.g)), t)),
            .b = @intFromFloat(std.math.lerp(@as(f32, @floatFromInt(a.b)), @as(f32, @floatFromInt(b.b)), t)),
            .a = @intFromFloat(std.math.lerp(@as(f32, @floatFromInt(a.a)), @as(f32, @floatFromInt(b.a)), t)),
        };
    }
};
const Vec2 = extern struct {
    x: f32 = 0,
    y: f32 = 0,

    pub fn add(self: Vec2, other: Vec2) Vec2 {
        return .{ .x = self.x + other.x, .y = self.y + other.y };
    }
    pub fn angle(self: Vec2) f32 {
        return std.math.atan2(f32, self.y, self.x);
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
    pub const PfnGetAtlasSubtextures = *const fn (this: *anyopaque, key: []const u8) *anyopaque;
    pub const PfnGetListCount = *const fn (this: *anyopaque) i32;
    pub const PfnListIndexDrawCentered = *const fn (this: *anyopaque, index: i32, x: f32, y: f32) void;
    pub const PfnListIndexRender = *const fn (this: *anyopaque, index: i32) void;
    pub const PfnDrawRect = *const fn (x: f32, y: f32, width: f32, hegith: f32, color: Color) void;

    const idle_bg_fill: Color = .{ .r = 0x47, .g = 0x40, .b = 0x70 };
    const moving_bg_fill: Color = .{ .r = 0x30, .g = 0xb3, .b = 0x35 };

    var entities: std.AutoHashMapUnmanaged(EntityID, Self) = .{};

    direction: Vec2 = .{},
    fill_color: Color = .{},

    fn init() Self {
        return .{};
    }
    fn deinit(self: Self) void {
        _ = self;
    }

    fn update(
        self: *Self,
        this: *anyopaque,
        delta_time: f32,
        position: *Vec2,
        base: PfnUpdate,
        move_h: PfnMoveH,
        move_v: PfnMoveV,
        collide_check_solid: PfnCollideCheckSolid,
    ) void {
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

        self.fill_color = Color.lerp(self.fill_color, if (self.direction.x != 0 or self.direction.y != 0) Self.moving_bg_fill else Self.idle_bg_fill, 10.0 * delta_time);
    }
    fn render(
        self: *Self,
        position: Vec2,
        extend: Vec2,
        arrow_sprites: *anyopaque,
        body_sprites: *anyopaque,
        get_list_count: PfnGetListCount,
        list_index_draw_centered: MovingBlockEntity.PfnListIndexDrawCentered,
        list_index_render: MovingBlockEntity.PfnListIndexRender,
        draw_rect: MovingBlockEntity.PfnDrawRect,
    ) void {
        const center = position.add(.{ .x = extend.x / 2.0, .y = extend.y / 2.0 });

        draw_rect(position.x + 3.0, position.y + 3.0, extend.x - 6.0, extend.y - 6.0, self.fill_color);
        const count = get_list_count(body_sprites);
        for (0..@intCast(count)) |i| {
            list_index_render(body_sprites, @intCast(i));
        }
        draw_rect(center.x - 4.0, center.y - 4.0, 8.0, 8.0, self.fill_color);

        if (self.direction.x != 0 or self.direction.y != 0) {
            list_index_draw_centered(arrow_sprites, std.math.clamp(@as(i32, @intFromFloat(@floor(@mod(-self.direction.angle() + std.math.tau, std.math.tau) / std.math.tau * 8.0 + 0.5))), 0, 7), center.x, center.y);
        } else {
            list_index_draw_centered(arrow_sprites, 8, center.x, center.y);
        }
    }
    fn on_dashed(self: *Self, direction: Vec2) DashCollisionResult {
        self.direction.x += direction.x;
        self.direction.y += direction.y;
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
    delta_time: f32,
    position: *Vec2,
    base: MovingBlockEntity.PfnUpdate,
    move_h: MovingBlockEntity.PfnMoveH,
    move_v: MovingBlockEntity.PfnMoveV,
    collide_check_solid: MovingBlockEntity.PfnCollideCheckSolid,
) void {
    MovingBlockEntity.entities.getPtr(id).?.update(this, delta_time, position, base, move_h, move_v, collide_check_solid);
}
pub export fn MovingBlockEntity_Render(
    id: EntityID,
    position: Vec2,
    extend: Vec2,
    arrow_sprites: *anyopaque,
    body_sprites: *anyopaque,
    get_list_count: MovingBlockEntity.PfnGetListCount,
    list_index_draw_centered: MovingBlockEntity.PfnListIndexDrawCentered,
    list_index_render: MovingBlockEntity.PfnListIndexRender,
    draw_rect: MovingBlockEntity.PfnDrawRect,
) void {
    MovingBlockEntity.entities.getPtr(id).?.render(position, extend, arrow_sprites, body_sprites, get_list_count, list_index_draw_centered, list_index_render, draw_rect);
}
pub export fn MovingBlockEntity_OnDashed(id: EntityID, direction: Vec2) DashCollisionResult {
    return MovingBlockEntity.entities.getPtr(id).?.on_dashed(direction);
}
