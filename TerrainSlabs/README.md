# Regex to clean localization files

`	"(?!block-(?:soil-|gravel-|sand-|rawclay-|cob-|forestfloor-|peat-|devastatedsoil-|glacierice|muddygravel|packedglacierice|sandwavy-|snowblock))(?:\\.|[^"\\])*":\s*"(?:\\.|[^"\\])*",?
`

# TODO

# Bugs

- Panning drops from other mods like Better Ruins are not added to terrain slabs (had to use "copy" operation to avoid JSON patching errors)

# Test cases

## Test suit: ice interaction

### Verify offset and particles
#### Steps
1. Get a lake ice block inside a terrain slab block (find a lake and type /time setmonth feb)
2. Place a torch on the lake ice

**Expected**: torch should be on ice, not inside it
**Expected**: particles from torch should spawn from the same place as if torch was on a full block

### Verify animatable blocks
#### Steps
1. Get a lake ice block inside a terrain slab block (find a lake and type /time setmonth feb)
2. Place a chest on the lake ice
3. Open and close it
4. Break the ice block, but keep the slab
5. Open and close it

**Expected**: chest should be placed ontop of ice
**Expected**: chest should move down after breaking the ice 
**Expected**: animations should be played on the correct y-level