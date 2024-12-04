shader_type canvas_item;

uniform float border_thickness = 2.0; // Thickness of the border
uniform vec4 border_color = vec4(0.0, 0.0, 0.0, 1.0); // Border color
uniform vec2 region_pos = vec2(0.0, 0.0); // Region position in the atlas
uniform vec2 region_size = vec2(1.0, 1.0); // Region size in the atlas

void fragment() {
    // Adjust UV to sample only within the defined region
    vec2 adjusted_uv = region_pos + UV * region_size;

    // Sample the alpha from the adjusted UVs
    float alpha = texture(TEXTURE, adjusted_uv).a;

    // Create the border effect
    float border = smoothstep(1.0 - border_thickness / 100.0, 1.0, alpha);

    // Mix the border color with the texture
    COLOR = mix(border_color, texture(TEXTURE, adjusted_uv), alpha);
}

void vertex() {
	// Called for every vertex the material is visible on.
}
//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
