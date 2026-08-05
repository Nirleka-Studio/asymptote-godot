shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

uniform float strength : hint_range(0.0, 5.0) = 1.0;
uniform float inner_radius : hint_range(0.0, 1.0) = 0.2; // Safe area in center
uniform int samples : hint_range(3, 16) = 8;             // Higher = smoother, lower = faster

void fragment() {
    vec2 center = vec2(0.5);
    vec2 uv = SCREEN_UV;
    
    // Calculate direction and distance from screen center
    vec2 dir = uv - center;
    float dist = length(dir);
    
    // Mask out center so chromatic aberration only hits the screen edges
    float mask = smoothstep(inner_radius, 1.0, dist);
    
    // Scale offset based on pixel size and radial mask
    vec2 max_offset = dir * (strength * 0.01) * mask;
    
    vec3 color_acc = vec3(0.0);
    vec3 weight_acc = vec3(0.0);
    
    // Sample across a spectral gradient using rainbow weights
    for (int i = 0; i < samples; i++) {
        float t = float(i) / float(samples - 1); // Normalized sample step (0.0 to 1.0)
        
        // Sample screen texture incrementally along the radial vector
        vec2 sample_uv = uv - max_offset * (t - 0.5);
        vec3 sample_color = texture(screen_texture, sample_uv).rgb;
        
        // Spectral weight curve (Cosine palette for smooth RGB color distribution)
        vec3 weight = 0.5 + 0.5 * cos(6.28318 * (t + vec3(0.0, 0.33, 0.67)));
        
        color_acc += sample_color * weight;
        weight_acc += weight;
    }
    
    // Normalize color output
    COLOR = vec4(color_acc / weight_acc, 1.0);
}