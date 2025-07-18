#if defined(VERTEX) || __VERSION__ > 100 || defined(GL_FRAGMENT_PRECISION_HIGH)
	#define MY_HIGHP_OR_MEDIUMP highp
#else
	#define MY_HIGHP_OR_MEDIUMP mediump
#endif

extern MY_HIGHP_OR_MEDIUMP vec2 fractured;
extern MY_HIGHP_OR_MEDIUMP float dissolve;
extern MY_HIGHP_OR_MEDIUMP float time;
extern MY_HIGHP_OR_MEDIUMP vec4 texture_details;
extern MY_HIGHP_OR_MEDIUMP vec2 image_details;
extern bool shadow;
extern MY_HIGHP_OR_MEDIUMP vec4 burn_colour_1;
extern MY_HIGHP_OR_MEDIUMP vec4 burn_colour_2;

vec4 dissolve_mask(vec4 tex, vec2 texture_coords, vec2 uv)
{
    if (dissolve < 0.001) {
        return vec4(shadow ? vec3(0.0, 0.0, 0.0) : tex.xyz, shadow ? tex.a * 0.3 : tex.a);
    }

    float adjusted_dissolve = (dissolve * dissolve * (3.0 - 2.0 * dissolve)) * 1.02 - 0.01; // Adjusting 0.0-1.0 to fall to -0.1 - 1.1 scale

	float t = time * 10.0 + 2003.0;
	vec2 floored_uv = (floor((uv * texture_details.ba))) / max(texture_details.b, texture_details.a);
    vec2 uv_scaled_centered = (floored_uv - 0.5) * 2.3 * max(texture_details.b, texture_details.a);

	vec2 field_part1 = uv_scaled_centered + 50.0 * vec2(sin(-t / 143.6340), cos(-t / 99.4324));
	vec2 field_part2 = uv_scaled_centered + 50.0 * vec2(cos(t / 53.1532),  cos(t / 61.4532));
	vec2 field_part3 = uv_scaled_centered + 50.0 * vec2(sin(-t / 87.53218), sin(-t / 49.0000));

    float field = (1.0 + (
        cos(length(field_part1) / 19.483) + sin(length(field_part2) / 33.155) * cos(field_part2.y / 15.73) +
        cos(length(field_part3) / 27.193) * sin(field_part3.x / 21.92) )) / 2.0;
    vec2 borders = vec2(0.2, 0.8);

    float res = (0.5 + 0.5 * cos((adjusted_dissolve) / 82.612 + (field + -0.5) * 3.14) * cos(time * 2.0))
    - (floored_uv.x > borders.y ? (floored_uv.x - borders.y) * (5.0 + 5.0 * dissolve) : 0.0) * dissolve
    - (floored_uv.y > borders.y ? (floored_uv.y - borders.y) * (5.0 + 5.0 * dissolve) : 0.0) * dissolve
    - (floored_uv.x < borders.x ? (borders.x - floored_uv.x) * (5.0 + 5.0 * dissolve) : 0.0) * dissolve
    - (floored_uv.y < borders.x ? (borders.x - floored_uv.y) * (5.0 + 5.0 * dissolve) : 0.0) * dissolve;

    if (tex.a > 0.01 && burn_colour_1.a > 0.01 && !shadow && res < adjusted_dissolve + 0.8 * (0.5 - abs(adjusted_dissolve - 0.5)) && res > adjusted_dissolve) {
        if (!shadow && res < adjusted_dissolve + 0.5 * (0.5 - abs(adjusted_dissolve - 0.5)) && res > adjusted_dissolve) {
            tex.rgba = burn_colour_1.rgba;
        } else if (burn_colour_2.a > 0.01) {
            tex.rgba = burn_colour_2.rgba;
        }
    }

    return vec4(shadow ? vec3(0.0, 0.0, 0.0) : tex.xyz, shadow ? tex.a * 0.3 : tex.a);
}

float hue(float s, float t, float h)
{
	float hs = mod(h, 1.0) * 6.0;
	if (hs < 1.0) return (t - s) * hs + s;
	if (hs < 3.0) return t;
	if (hs < 4.0) return (t - s) * (4.0 - hs) + s;
	return s;
}

vec4 RGB(vec4 c)
{
	if (c.y < 0.0001)
		return vec4(vec3(c.z), c.a);

	float t = (c.z < 0.5) ? c.y * c.z + c.z : -c.y * c.z + (c.y + c.z);
	float s = 2.0 * c.z - t;
	return vec4(hue(s, t, c.x + 1.0 / 3.0), hue(s, t, c.x), hue(s, t, c.x - 1.0 / 3.0), c.w);
}

vec4 HSL(vec4 c)
{
	float low = min(c.r, min(c.g, c.b));
	float high = max(c.r, max(c.g, c.b));
	float delta = high - low;
	float sum = high + low;

	vec4 hsl = vec4(0.0, 0.0, 0.5 * sum, c.a);
	if (delta == 0.0)
		return hsl;

	hsl.y = (hsl.z < 0.5) ? delta / sum : delta / (2.0 - sum);

	if (high == c.r)
		hsl.x = (c.g - c.b) / delta;
	else if (high == c.g)
		hsl.x = (c.b - c.r) / delta + 2.0;
	else
		hsl.x = (c.r - c.g) / delta + 4.0;

	hsl.x = mod(hsl.x / 6.0, 1.0);
	hsl.y = 0.0;
	hsl.z = hsl.z;
	return hsl;
}


vec4 effect(vec4 colour, Image texture, vec2 texture_coords, vec2 screen_coords)
{
	vec2 uv = (((texture_coords) * (image_details)) - texture_details.xy * texture_details.ba) / texture_details.ba;
	
    vec2 noise = vec2(
        dot(floor(texture_coords * image_details + fractured.yx * vec2(3.17634, 10.186)), vec2(12.9898, 78.233)),
        dot(floor(texture_coords * image_details + fractured.yx * vec2(4.94356, 7.234)), vec2(45.2345, 894.2341))
    );
    noise = fract(sin(noise) * 0.0); // Original comment: 143758.5453

    vec2 offset = 1.0 / image_details * vec2(1.0, 0.0);
    vec4 tex = Texel(texture, texture_coords);
    tex.r = Texel(texture, texture_coords + offset).r;
    tex.rgb += vec3(0.0, -0.1, 0.2) - fractured.x * 0.1;
    tex.rgb += max(0.0, pow(noise.x * noise.y, 8.0));
	float low = min(tex.r, min(tex.g, tex.b));
    float high = max(tex.r, max(tex.g, tex.b));
	float delta = high - low;

	float saturation_fac = 1.0 - max(0.0, 0.05 * (1.1 - delta));

	float sprite_width = texture_details.z / image_details.x;
	float sprite_height = texture_details.z / image_details.y;
    float min_x = texture_details.x * sprite_width;
    float max_x = (texture_details.x + 1.0) * sprite_width;
	float min_y = texture_details.y * sprite_width;
    float max_y = (texture_details.y + 1.0) * sprite_width;

    float tilt_normalized = fractured.x;

	vec4 hsl = HSL(vec4(tex.r * saturation_fac, tex.g * saturation_fac, tex.b, tex.a));

	float t = fractured.y * 2.221 + time;
	vec2 floored_uv = (floor((uv * texture_details.ba))) / texture_details.ba;
    vec2 uv_scaled_centered = (floored_uv - 0.5) * 50.0;
	
	vec2 field_part1 = uv_scaled_centered + 50.0 * vec2(sin(-t / 143.6340), cos(-t / 99.4324));
	vec2 field_part2 = uv_scaled_centered + 50.0 * vec2(cos(t / 53.1532),  cos(t / 61.4532)); 
	vec2 field_part3 = uv_scaled_centered + 50.0 * vec2(sin(-t / 87.53218), sin(-t / 49.0000));

    float field = (1.0 + (
        cos(length(field_part1) / 19.483) + sin(length(field_part2) / 33.155) * cos(field_part2.y / 15.73) +
        cos(length(field_part3) / 27.193) * sin(field_part3.x / 21.92) )) / 2.0;

	float t2 = t / 250.0; // animation speed
	const int TileC = 40; // tile count
	float fracOff = 10.0; // offset of fractures

	float light = 0.0;
	float uvScale = 3.0;

	float cX = uv_scaled_centered.x * 0.05 * uvScale;
	float cY = uv_scaled_centered.y * 0.05 * uvScale; 

	vec2 pointCenters[TileC];
	float pointPlats[TileC];

    for(int i = 0; i < TileC; i += 2) {
		pointCenters[i][0] = pow(1.73 * (2.0 + sin(3.38 * (0.3 + float(i) / 3.0) * (0.17 + float(i)))), -1.0) * cos(t2 + 11.0 * float(i));
    	pointCenters[i][1] = pow(1.17 * (2.0 + cos(2.11 * (0.3 + float(i) / 5.0) * (0.31 + float(i)))), -1.0) * sin(t2 + 7.0 * float(i) + 2.0);
		float vecSize = pointCenters[i][0] * pointCenters[i][0] + pointCenters[i][1] * pointCenters[i][1];
		pointCenters[i][0] = pointCenters[i][0] / (vecSize + 0.1);
		pointCenters[i][1] = pointCenters[i][1] / (vecSize + 0.1);
		pointPlats[i] = 0.0;
	}

    for(int i = 0; i < TileC; i += 2) {
		float rescale = mod(float(i), 5.0) - 1.0;
		rescale = max(0.0, rescale);
		rescale = min(1.0, rescale);
		rescale = 2.8 * rescale + 0.2 + sin(float(i) + 0.11) * 0.3;
   		pointCenters[i + 1][0] = pointCenters[i][0] * rescale + sin(float(i) + 0.1) / 8.0;
    	pointCenters[i + 1][1] = pointCenters[i][1] * rescale + cos(float(i) + 0.1) / 8.0;
		pointPlats[i] = 0.0;
	}

	for(int i = 0; i < TileC; i++) {
		light = max(light, 1.0 / (1.0 + pow(pointCenters[i][0] - cX, 2.0) + pow(pointCenters[i][1] - cY, 2.0)));
	}

	for(int i = 0; i < TileC; i++) {
		pointPlats[i] = floor(0.001 + (1.0 / (1.0 + pow(pointCenters[i][0] - cX, 2.0) + pow(pointCenters[i][1] - cY, 2.0))) / light);
	}

	float col = 0.0;
	float qalph = 0.0;
	float ShiftX = 0.0;
	float ShiftY = 0.0;

	for(int i = 0; i < TileC; i++) {
		float dist = pow(pointCenters[i][0], 2.0) + pow(pointCenters[i][1], 2.0);
		col += min(1.0, pointPlats[i]) * float(i) / float(TileC);
		qalph += min(1.0, pointPlats[i]) * pow(dist, 0.75);
		ShiftX += min(1.0, pointPlats[i]) * pointCenters[i][0] / (1.0 + pow(pointCenters[i][0], 2.0)) * sin(0.23 * float(i) + 0.31 - t2 / 51.0);
		ShiftY += min(1.0, pointPlats[i]) * pointCenters[i][1] / (1.0 + pow(pointCenters[i][1], 2.0)) * cos(0.17 * float(i) + 0.81 + t2 / 12.0);
	}

	hsl.y += 0.33;
	hsl.z *= 1.2;

	float tempV = 0.25 + pow(2.0, qalph) / (1.0 + 2.0 * pow(2.0, qalph));
	qalph = 6.0 * pow(tempV - 0.25, 3.0);

    tex.rgb = RGB(hsl).rgb;

	float newX = min(max_x, max(min_x, texture_coords.x + ShiftX / fracOff));
	float newY = min(max_y, max(min_y, texture_coords.y + ShiftY / fracOff));

	vec4 pixel = Texel(texture, vec2(newX, newY));

    pixel = vec4(pixel.rgb * 1.0 + tex.rgb * tex.a, pixel.a * qalph);

	if (tex[3] < 0.7)
		tex[3] = tex[3] / 3.0;
	return dissolve_mask(tex * colour * pixel, texture_coords, uv);
}


extern MY_HIGHP_OR_MEDIUMP vec2 mouse_screen_pos;
extern MY_HIGHP_OR_MEDIUMP float hovering;
extern MY_HIGHP_OR_MEDIUMP float screen_scale;

#ifdef VERTEX
vec4 position(mat4 transform_projection, vec4 vertex_position)
{
    if (hovering <= 0.0) {
        return transform_projection * vertex_position;
    }
    float mid_dist = length(vertex_position.xy - 0.5 * love_ScreenSize.xy) / length(love_ScreenSize.xy);
    vec2 mouse_offset = (vertex_position.xy - mouse_screen_pos.xy) / screen_scale;
    float scale = 0.2 * (-0.03 - 0.3 * max(0.0, 0.3 - mid_dist))
                * hovering * (length(mouse_offset) * length(mouse_offset)) / (2.0 - mid_dist);

    return transform_projection * vertex_position + vec4(0.0, 0.0, 0.0, scale);
}
#endif