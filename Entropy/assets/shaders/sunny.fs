#if defined(VERTEX) || __VERSION__ > 100 || defined(GL_FRAGMENT_PRECISION_HIGH)
	#define MY_HIGHP_OR_MEDIUMP highp
#else
	#define MY_HIGHP_OR_MEDIUMP mediump
#endif

extern MY_HIGHP_OR_MEDIUMP vec2 sunny;
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
        return vec4(shadow ? vec3(0.0,0.0,0.0) : tex.xyz, shadow ? tex.a*0.3 : tex.a);
    }

    float adjusted_dissolve = (dissolve*dissolve*(3.0-2.0*dissolve))*1.02 - 0.01; //Adjusting 0.0-1.0 to fall to -0.1 - 1.1 scale so the mask does not pause at extreme values

	float t = time * 10.0 + 2003.0;
	vec2 floored_uv = (floor((uv*texture_details.ba)))/max(texture_details.b, texture_details.a);
    vec2 uv_scaled_centered = (floored_uv - 0.5) * 2.3 * max(texture_details.b, texture_details.a);

	vec2 field_part1 = uv_scaled_centered + 50.0*vec2(sin(-t / 143.6340), cos(-t / 99.4324));
	vec2 field_part2 = uv_scaled_centered + 50.0*vec2(cos( t / 53.1532),  cos( t / 61.4532));
	vec2 field_part3 = uv_scaled_centered + 50.0*vec2(sin(-t / 87.53218), sin(-t / 49.0000));

    float field = (1.0 + (
        cos(length(field_part1) / 19.483) + sin(length(field_part2) / 33.155) * cos(field_part2.y / 15.73) +
        cos(length(field_part3) / 27.193) * sin(field_part3.x / 21.92) ))/2.0;
    vec2 borders = vec2(0.2, 0.8);

    float res = (0.5 + 0.5* cos( (adjusted_dissolve) / 82.612 + ( field + -0.5 ) *3.14))
    - (floored_uv.x > borders.y ? (floored_uv.x - borders.y)*(5.0 + 5.0*dissolve) : 0.0)*(dissolve)
    - (floored_uv.y > borders.y ? (floored_uv.y - borders.y)*(5.0 + 5.0*dissolve) : 0.0)*(dissolve)
    - (floored_uv.x < borders.x ? (borders.x - floored_uv.x)*(5.0 + 5.0*dissolve) : 0.0)*(dissolve)
    - (floored_uv.y < borders.x ? (borders.x - floored_uv.y)*(5.0 + 5.0*dissolve) : 0.0)*(dissolve);

    if (tex.a > 0.01 && burn_colour_1.a > 0.01 && !shadow && res < adjusted_dissolve + 0.8*(0.5-abs(adjusted_dissolve-0.5)) && res > adjusted_dissolve) {
        if (!shadow && res < adjusted_dissolve + 0.5*(0.5-abs(adjusted_dissolve-0.5)) && res > adjusted_dissolve) {
            tex.rgba = burn_colour_1.rgba;
        } else if (burn_colour_2.a > 0.01) {
            tex.rgba = burn_colour_2.rgba;
        }
    }

    return vec4(shadow ? vec3(0.0,0.0,0.0) : tex.xyz, res > adjusted_dissolve ? (shadow ? tex.a*0.3 : tex.a) : 0.0);
}

float hue(float s, float t, float h)
{
	float hs = mod(h, 1.0)*6.0;
	if (hs < 1.0) return (t-s) * hs + s;
	if (hs < 3.0) return t;
	if (hs < 4.0) return (t-s) * (4.0-hs) + s;
	return s;
}

vec4 RGB(vec4 c)
{
	if (c.y < 0.0001)
		return vec4(vec3(c.z), c.a);

	float t = (c.z < 0.5) ? c.y*c.z + c.z : -c.y*c.z + (c.y+c.z);
	float s = 2.0 * c.z - t;
	return vec4(hue(s,t,c.x + 1.0/3.0), hue(s,t,c.x), hue(s,t,c.x - 1.0/3.0), c.w);
}

vec4 HSL(vec4 c)
{
	float low = min(c.r, min(c.g, c.b));
	float high = max(c.r, max(c.g, c.b));
	float delta = high - low;
	float sum = high+low;

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
	return hsl;
}

vec4 effect( vec4 colour, Image texture, vec2 texture_coords, vec2 screen_coords )
{
    vec4 tex = Texel(texture, texture_coords);
	vec2 uv = (((texture_coords)*(image_details)) - texture_details.xy*texture_details.ba)/texture_details.ba;

	float low = min(tex.r, min(tex.g, tex.b));
    float high = max(tex.r, max(tex.g, tex.b));
	float delta = high - low;

	float saturation_fac = 1.0 - max(0.0, 0.05*(1.1-delta));

	vec4 hsl = HSL(vec4(tex.r*saturation_fac, tex.g*saturation_fac, tex.b, tex.a));

	float t = sunny.y*2.221 + time;
	vec2 floored_uv = (floor((uv*texture_details.ba)))/texture_details.ba;
    vec2 uv_scaled_centered = (floored_uv - 0.5) * 50.0;

	vec2 field_part1 = uv_scaled_centered + 50.0*vec2(sin(-t / 13.6340), cos(-t / 9.4324));
	vec2 field_part2 = uv_scaled_centered + 50.0*vec2(cos( t / 5.1532),  cos( t / 6.4532));
	vec2 field_part3 = uv_scaled_centered + 50.0*vec2(sin(-t / 8.53218), sin(-t / 4.0000));

    float field = (1.0 + (
        cos(length(field_part1) / 19.483) + sin(length(field_part2) / 33.155) * cos(field_part2.y / 15.73) +
        cos(length(field_part3) / 27.193) * sin(field_part3.x / 21.92) ))/2.0;
	
    float res = (0.5 + 0.5* cos( (sunny.x) * 2.612 + ( field + -0.5 ) *3.14));


	float sun1[3] = float[3](0.119, 1.0, 0.65);
	float sun2[3] = float[3](0.094, 0.91, 0.5);
	float c0 = (sun1[0] + sun2[0])/2.0;

	float closestHue = ceil(mod(hsl.x - c0, 1.0) - 0.5);
	float H2 = closestHue * sun2[0] + (1.0 - closestHue) * sun1[0];
	float S2 = closestHue * sun2[1] + (1.0 - closestHue) * sun1[1];
	float L2 = closestHue * sun2[2] + (1.0 - closestHue) * sun1[2];



	float t2 = t/8.0;

	


	float pulser = min(floor(mod(t2, 7.0)), 1.0);
	float pulseRise = pulser * (mod(t2, 7.0) - 1.0)/6.0;
	float pulseDrop = (1.0 - pulser) * mod(-t2, 1.0);
	float pulse = pow(pow(pulseRise, 4.0) + pow(pulseDrop, 4.0), 4.0);



	float q3 = 2.0 * (floor(0.5 + t2/2.0) - floor(t2/2.0)) - 1.0;
	float q4 = sin(t2 * (5.0 - 4.0 * pulse)/100.0) * sin((3.0 * t2/1.73 + 1.0 + field * 2.0)*4.0)/2.5;

	vec4 pixel = Texel(texture, texture_coords);

	hsl.x = hsl.x * pulse + (1.0 - pulse) * H2;
	hsl.y = hsl.y * pulse + (1.0 - pulse) * (S2 + q4/7.0);
	hsl.z = hsl.z * pulse + (1.0 - pulse) * (L2 + q4/12.0);


    tex.rgb = RGB(hsl).rgb;
	pixel = vec4(pixel.rgb * 0.66 + tex.rgb * tex.a, pixel.a * 0.9);

	if (tex[3] < 0.7)
		tex[3] = tex[3]/3.0;
	return dissolve_mask(tex*pixel, texture_coords, uv);
}

extern MY_HIGHP_OR_MEDIUMP vec2 mouse_screen_pos;
extern MY_HIGHP_OR_MEDIUMP float hovering;
extern MY_HIGHP_OR_MEDIUMP float screen_scale;

#ifdef VERTEX
vec4 position( mat4 transform_projection, vec4 vertex_position )
{
    if (hovering <= 0.0){
        return transform_projection * vertex_position;
    }
    float mid_dist = length(vertex_position.xy - 0.5*love_ScreenSize.xy)/length(love_ScreenSize.xy);
    vec2 mouse_offset = (vertex_position.xy - mouse_screen_pos.xy)/screen_scale;
    float scale = 0.2*(-0.03 - 0.3*max(0.0, 0.3-mid_dist))
                *hovering*(length(mouse_offset)*length(mouse_offset))/(2.0 - mid_dist);

    return transform_projection * vertex_position + vec4(0.0, 0.0, 0.0, scale);
}
#endif