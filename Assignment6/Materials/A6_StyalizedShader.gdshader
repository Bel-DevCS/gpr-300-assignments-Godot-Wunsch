shader_type spatial;
render_mode cull_back, depth_draw_opaque;

uniform vec4 albedo_color : source_color = vec4(1.0, 0.8, 0.6, 1.0);
uniform int shading_levels : hint_range(1, 6) = 3;
uniform float rim_strength : hint_range(0.0, 1.0) = 0.3;
uniform float gradient_mix : hint_range(0.0, 1.0) = 0.6;

varying vec3 vert_normal;
varying vec3 vert_position;
varying vec3 vert_color;

void vertex() {
    vert_normal = NORMAL;
    vert_position = VERTEX;

    // Simulated gradient fallback (if no vertex colors)
    float gradient = clamp((VERTEX.y + 1.0) * 0.5, 0.0, 1.0);
    vert_color = mix(vec3(0.4), vec3(1.0), gradient);
}

void fragment() {
    ALBEDO = mix(albedo_color.rgb, vert_color, gradient_mix);
    ALPHA = albedo_color.a;
}

void light() {
    vec3 n = normalize(vert_normal);
    vec3 l = normalize(LIGHT);
    vec3 v = normalize(VIEW);

    float diff = max(dot(n, l), 0.0);
    float stepSize = 1.0 / float(shading_levels);
    float toon = floor(diff / stepSize) * stepSize;

    float rim = pow(1.0 - max(dot(n, v), 0.0), 2.0) * rim_strength;

    vec3 result = toon * LIGHT_COLOR.rgb + rim;
    DIFFUSE_LIGHT = ALBEDO * min(result, vec3(1.0));
}
