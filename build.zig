const std = @import("std");

const name = "NativeLibraryMod";
const path = "bin";

const Target = struct {
    arch_os: []const u8,
    filename: []const u8,
    dst_path: []const u8,
};

pub fn build(b: *std.Build) !void {
    const optimize = b.standardOptimizeOption(.{});

    const install_prefix = b.getInstallPath(.{ .lib = {} }, "");
    const targets = [_]Target{
        // .{ .arch_os = "x86_64-windows", .filename = b.fmt("{s}.dll", .{name}), .dst_path = b.pathJoin(&.{ path, "lib-win-x64" }) },
        .{ .arch_os = "x86_64-linux", .filename = b.fmt("lib{s}.so", .{name}), .dst_path = b.pathJoin(&.{ path, "lib-linux" }) },
        // .{ .arch_os = "x86_64-macos", .filename = b.fmt("lib{s}.dylib", .{name}), .dst_path = b.pathJoin(&.{ path, "lib-osx" }) },
    };
    for (targets) |t| {
        const lib = b.addSharedLibrary(.{
            .name = name,
            .root_source_file = .{ .path = "src/main.zig" },
            .target = try std.zig.CrossTarget.parse(.{ .arch_os_abi = t.arch_os }),
            .optimize = optimize,
        });

        const install_step = b.addInstallArtifact(lib, .{});
        b.getInstallStep().dependOn(&install_step.step);

        // Why is there no copy files step?
        const mkdir_cmd = b.addSystemCommand(&.{ "mkdir", "-p", t.dst_path });
        const cp_cmd = b.addSystemCommand(&.{ "cp", b.pathJoin(&.{ install_prefix, t.filename }), b.pathJoin(&.{ t.dst_path, t.filename }) });
        cp_cmd.step.dependOn(&mkdir_cmd.step);
        cp_cmd.step.dependOn(&install_step.step);
        b.getInstallStep().dependOn(&cp_cmd.step);

        // const copy_step = b.step(b.fmt("copy-{s}", .{t.arch_os}), "Copy libraries into correct directory");
        // copy_step.dependOn(&cp_cmd.step);
    }

    const package_cmd = b.addSystemCommand(&.{
        "zip",
        b.fmt("{s}.zip", .{name}),
        "everest.yaml",
        b.fmt("{s}/{s}.dll", .{ path, name }),
        b.fmt("{s}/{s}.pdb", .{ path, name }),
        b.fmt("{s}/lib-win-x64/{s}.dll", .{ path, name }),
        b.fmt("{s}/lib-linux/lib{s}.so", .{ path, name }),
        b.fmt("{s}/lib-osx/lib{s}.dylib", .{ path, name }),
    });
    package_cmd.step.dependOn(b.getInstallStep());
    const package_step = b.step("package", "Package everything into a mod ZIP");
    package_step.dependOn(&package_cmd.step);
}
