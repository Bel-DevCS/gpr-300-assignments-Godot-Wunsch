shader_type spatial;
render_mode cull_back, depth_draw_opaque;

uniform vec4 albedo_color : source_color = vec4(1.0, 0.8, 0.6, 1.0);
uniform int shading_levels : hint_range(1, 6) = 2;
uniform float rim_strength : hint_range(0.0, 2.0) = 1.0;
uniform float gradient_mix : hint_range(0.0, 1.0) = 0.5;

varying vec3 vert_normal;
varying vec3 vert_position;
varying vec3 vert_color;

void vertex() {
    vert_normal = NORMAL;
    vert_position = VERTEX;

    // Gradient (based on Y)
    float gradient = clamp((VERTEX.y + 1.0) * 0.5, 0.0, 1.0);
    vert_color = mix(vec3(0.3), vec3(1.0), gradient);
}

void fragment() {
    ALBEDO = mix(albedo_color.rgb, vert_color, gradient_mix);
    ALPHA = albedo_color.a;
}

void light() {
    vec3 N = normalize(vert_normal);
    vec3 L = normalize(LIGHT);
    vec3 V = normalize(VIEW);

    float diff = max(dot(N, L), 0.0);
    float step = 1.0 / float(shading_levels);
    float toon = floor(diff / step) * step;

    float rim = pow(1.0 - dot(N, V), 2.0) * rim_strength;

    vec3 lighting = toon * LIGHT_COLOR.rgb + rim;

    DIFFUSE_LIGHT = ALBEDO * clamp(lighting, 0.0, 1.0);
}
