# Night scenery, fog and vehicle lights

Requested 2026-11-03: visible moon, working fog, fuller surroundings and front/rear vehicle lights. The Input Actions and driving controls are unchanged.

## Evidence and changes

The original moon projected above the driving viewport (viewport y approximately 1.47). Its direction is now lower and its disc has a soft halo and subtle surface variation. The reviewed Game view shows it above the left roadside.

The existing car damage effect wrote a black emission color to every renderer each frame, including the authored taillamps. VehicleLights now owns fitted lenses on all five car models, road-facing spotlights, red taillamps, stronger braking lamps and white reverse lamps. CarDriver excludes lamp renderers from its damage flash. Stationary and merging traffic light up when activated. The merging indicator is resolved by name so added headlights cannot be mistaken for it.

Close-up visual review also exposed a material validation issue: a nonzero property-block emission value cannot light a lens when the material's emission shader keyword is disabled. URP's material validation checks the AnyEmissive flags. Lamp authoring now sets an emissive flag and nonblack shared color before enabling the keyword; VehicleLights still controls visible intensity through property blocks. The integration check asserts that every car's lens material retains the emission keyword.

The original RenderSettings fog state changed with weather, but forcing its density did not create a clear visible difference in the captured URP view. This did not establish a specific Unity defect. The game now uses depth-reconstructed distance/height fog through Unity's built-in fullscreen renderer feature before post-processing. The sky uses matching horizon haze. RenderSettings fog is disabled to prevent double application; its density value still exposes weather state. Same-camera clear/dense captures visibly confirm the replacement. Early fog occurs during the first road section as well as later weather zones.

Road sections now prewarm denser trees, bushes, grass, rocks, houses and larger buildings, plus faceted hills beyond the roadside. Town trees stay behind the buildings instead of disappearing. Separate rows on both sides replace sparse diagonal placement. Recycling repositions these existing objects.

## Review

Visual captures are staged for inspection; automated PlayMode tests exercise the real New Input System and light/weather behavior separately. See Validation.md and the raw results in Validation for the completed checks and remaining limits.
