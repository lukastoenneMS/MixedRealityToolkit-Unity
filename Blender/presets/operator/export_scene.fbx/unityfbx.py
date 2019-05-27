import bpy
op = bpy.context.active_operator

op.axis_forward = '-Y'
op.axis_up = 'Z'
op.use_selection = True
op.global_scale = 1.0
op.apply_unit_scale = False
op.apply_scale_options = 'FBX_SCALE_ALL'
op.bake_space_transform = True
