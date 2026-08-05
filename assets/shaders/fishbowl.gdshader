shader_type canvas_item;

// Important: must use screen_texture hint
uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

// exposed controls
uniform float fishbowl_strength : hint_range(-2.0, 2.0) = 0.05; // 0 is no distortion, >0 is barrel/fishbowl, <0 is pincushion
uniform float chroma_strength : hint_range(0.0, 5.0) = 1.0;
uniform int chroma_samples : hint_range(3, 16) = 8;
uniform float scale = 1.05; // Slightly zoom in to hide black edges caused by distortion

void fragment() {
    // 1. Center UVs [-1.0 to 1.0]
    vec2 center = vec2(0.5);
    vec2 p = SCREEN_UV - center;
    
    // 2. Apply Fishbowl Distortion (Physical Curvature)
    // We adjust 'p' based on distance cubed to bow the image outwards.
    float dist = length(p);
    float dist_sq = dist * dist;
    // New coordinate space, bent radially
    vec2 distorted_uv_p = p * (1.0 + dist_sq * fishbowl_strength);
    
    // Convert back to [0.0 to 1.0] SCREEN_UV space and scale slightly
    vec2 distorted_uv = distorted_uv_p / scale + center;
    
    // 3. Apply Spectral Chromatic Aberration (along the distorted paths)
    vec2 dir = distorted_uv - center;
    float final_dist = length(dir);
    
    // The direction and strength of color offset
    vec2 max_offset = dir * (chroma_strength * 0.005);
    
    vec3 color_acc = vec3(0.0);
    vec3 weight_acc = vec3(0.0);
    
    for (int i = 0; i < chroma_samples; i++) {
        float t = float(i) / float(chroma_samples - 1);
        
        // Sample screen texture incrementally along the radial vector
        // t-0.5 centers the aberration (middle stays sharp, edges blur)
        vec2 sample_uv = distorted_uv - max_offset * (t - 0.5);
        vec3 sample_color = texture(screen_texture, sample_uv).rgb;
        
        // Spectral weight curve (Cosine palette for smooth RGB dispersion)
        vec3 weight = 0.5 + 0.5 * cos(6.28318 * (t + vec3(0.0, 0.33, 0.67)));
        
        color_acc += sample_color * weight;
        weight_acc += weight;
    }
    
    COLOR = vec4(color_acc / weight_acc, 1.0);
}