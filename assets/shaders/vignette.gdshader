shader_type canvas_item;

uniform vec4 vignette_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float inner_radius : hint_range(0.0, 1.0) = 0.2;
uniform float outer_radius : hint_range(0.0, 1.5) = 0.65;
uniform float smoothness : hint_range(0.1, 5.0) = 1.0;
uniform float dither_strength : hint_range(0.0, 0.1) = 0.03; // Controls noise strength to eliminate lines

// Simple hash for screen-space dither noise
float random(vec2 uv) {
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453123);
}

void fragment() {
    float dist = length(UV - vec2(0.5));
    float factor = smoothstep(inner_radius, outer_radius, dist);
    factor = pow(factor, smoothness);
    
    // Generate subtle noise per pixel to mask color quantization banding
    float noise = (random(FRAGCOORD.xy) - 0.5) * dither_strength;
    factor = clamp(factor + noise, 0.0, 1.0);
    
    COLOR = vec4(vignette_color.rgb, factor * vignette_color.a);
}