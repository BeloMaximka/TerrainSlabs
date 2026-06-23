# TODO

# Bugs

- Incorrect break overlay when breaking an ice lake block with a terrain slab inside
- Stackable ground storage (fireclay bricks, firewood etc) is floating on slabs
- Snowed grass ontop of lake ice with a terrain slab should be inside the ice, but has its snow layer on top of the ice
- Bamboo and redwood don't replace the slabs underneath them with full blocks when they grow from a sapling
- Collision boxes are not offset for blocks that override GetCollisionBoxes() (BlockGroundStorage for example)

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

## Test suit: decors
### Verify decor offset
#### Steps
1. Place a terrain slab 
2. Place a full block above it
3. Place a decor layer block on the side of the full block

**Expected**: decor should fully cover the side of the full block and not be offset lower/higher