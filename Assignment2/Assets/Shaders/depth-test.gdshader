shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always;

uniform float camera_near : hint_range(0.1, 100.0) = 0.1;
uniform float camera_far  : hint_range(0.1, 1000.0) = 1000.0;


varying float v_depth;

void vertex() {
    vec4 viewPos = MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
    // No need to invert sign if your camera is set up differently.
    v_depth = viewPos.z;
}

void fragment() {
    // Map the depth value (assuming camera_near and camera_far are positive).
    float depthNorm = clamp((v_depth - camera_near) / (camera_far - camera_near), 0.0, 1.0);

    // Output the depth as a grayscale color.
    ALBEDO = vec3(depthNorm);
}