import bpy
from bpy.types import Operator
from math import *
from . import fracture_cell


bl_info = {
    "name": "Puzzle Utils",
    "author": "Lukas Toenne",
    "version": (0, 1),
    "blender": (2, 79, 0),
    "location": "3D View tools",
    "description": "Prepare an object for use as a puzzle",
    "warning": "",
    "category": "Object"}


def select_single_object(ob):
    for tob in bpy.data.objects:
        tob.select = (tob == ob)


def select_object_list(obs):
    for tob in bpy.data.objects:
        tob.select = False
    for tob in obs:
        tob.select = True


def MakeFracturePuzzle(ob):
    pass


class CreateUnityColliders(Operator):
    bl_idname = "puzzle.create_unity_colliders"
    bl_label = "Create Unity Colliders"
    bl_options = {'PRESET'}

    def execute(self, context):
        if not context.selected_objects:
            return {'CANCELLED'}

        scene = context.scene

        container = bpy.data.objects.new("Shards", None)
        scene.objects.link(container)
        container.parent = context.selected_objects[0].parent

        cache_selected_objects = context.selected_objects[:]
        cache_active_object = context.active_object

        print("Creating Puzzle Colliders:")
        for meshOb in cache_selected_objects:
            if meshOb.type != 'MESH':
                continue

            basename = meshOb.name
            print("Shard: ", basename)

            shard = bpy.data.objects.new(basename + ".Shard", None)
            scene.objects.link(shard)
            shard.parent = container

            # Center the shard on the mesh object
            shard.matrix_world = meshOb.matrix_world
            meshOb.parent = shard
            meshOb.matrix_world = shard.matrix_world
            meshOb.name = basename + ".Render"

            collOb = meshOb.copy()
            collOb.data = meshOb.data.copy()
            scene.objects.link(collOb)
            collOb.parent = shard
            collOb.name = basename + ".Collider"

            scene.objects.active = collOb
            select_single_object(collOb)
            bpy.ops.object.mode_set(mode='EDIT')
            bpy.ops.mesh.remove_doubles()
            bpy.ops.object.mode_set(mode='OBJECT')

            mesh = collOb.data
            mod = collOb.modifiers.new(name="Decimate", type='DECIMATE')
            mod.decimate_type = 'COLLAPSE'
            # Number of triangles is: sum(numLoops - 2) = totLoops - 2 * totPolys
            totTris = len(mesh.loops) - 2 * len(mesh.polygons)
            if totTris > 0:
                mod.ratio = 255.0 / float(totTris)
            else:
                mod.ratio = 1.0
            mod.use_collapse_triangulate = True

            scene.objects.active = collOb
            bpy.ops.object.modifier_apply(apply_as='DATA', modifier=mod.name)
            assert(len(collOb.data.polygons) <= 255)

            # select_single_object(meshOb)
            # scene.objects.active = meshOb

        # select_object_list(cache_selected_objects)
        # scene.objects.active = cache_active_object

        return {'FINISHED'}


def register():
    bpy.utils.register_class(CreateUnityColliders)


def unregister():
    bpy.utils.unregister_class(CreateUnityColliders)


if __name__ == "__main__":
    register()
