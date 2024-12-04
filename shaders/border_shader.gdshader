shader_type canvas_item;

uniform float border_thickness : hint_range(0.0, 10.0) = 2.0;
uniform vec4 border_color : hint_color = vec4(0.0, 0.0, 0.0, 1.0);

void fragment() {
    // Get the alpha value of the current pixel
    float alpha = texture(TEXTURE, UV).a;

    // Get the distance to the edge of the alpha mask
    float dist = 1.0 - smoothstep(1.0 - border_thickness / 100.0, 1.0, alpha);

    // Set the fragment color
    if (dist > 0.0) {
        COLOR = border_color;
    } else {
        COLOR = texture(TEXTURE, UV);
    }
}