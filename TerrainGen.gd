class_name ChunkGrid
extends Node3D

var gridSize: int = 16
var chunks: Array[Chunk] = []
var chunkPositions: Array[Vector3i] = []
var rng: RandomNumberGenerator = RandomNumberGenerator.new()

# Called when the node enters the scene tree for the first time.
func _ready():
	rng.seed = hash("Emergent")
	_generate(Vector3i(0,0,0))

func _generate(pos: Vector3i):
	# in a radius of 1 around the current chunk
	var curPos: Vector3i
	var chunkIndex: int
	for x in 3:
		for y in 3:
			for z in 3:
				# update current position
				curPos = Vector3i(pos.x+x-1, pos.y+y-1, pos.z+z-1)
				# check if chunk exists already
				chunkIndex = chunkPositions.find(curPos)
				if (chunkIndex == -1):
					# chunk doesn't exist yet, so create
					var newChunk: Chunk = Chunk.new(curPos, gridSize, self)
					chunkPositions.append(curPos)
					chunks.append(newChunk)
	curPos = Vector3i(pos.x, pos.y, pos.z)
	chunkIndex = chunkPositions.find(curPos)
	if (chunks[chunkIndex].generated == false):
		# chunk is not generated yet, so generate
		chunks[chunkIndex]._generate()

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass

func _getChunk(pos: Vector3i):
	pass

class Chunk:
	var position: Vector3i
	var transform: Transform3D
	var generated: bool
	var gridSize: int
	var chunkGrid: ChunkGrid
	var grid: Array[PackedVector3Array] = []
	const groundChance: float = .5
	
	func _init(pos: Vector3i, gSize: int, cGrid: ChunkGrid):
		position = pos
		gridSize = gSize
		chunkGrid = cGrid
		transform = Transform3D(Basis(), Vector3(pos.x*gSize, pos.y*gSize, pos.z*gSize))
		_create()
	
	func _create():
		# TODO: pregenerate chunk but don't smoothen yet
		var rng: RandomNumberGenerator = chunkGrid.rng
		for x in gridSize:
			for y in gridSize:
				for z in gridSize:
					var value: bool = rng.randf() < groundChance
					pass
	
	func _generate():
		# TODO: actually generate chunk
		var rng: RandomNumberGenerator = chunkGrid.rng
		var curPos: Vector3i
		rng.state = hash(position)
		for x in gridSize:
			for y in gridSize:
				for z in gridSize:
					curPos = Vector3i(x-1,y-1,z-1)
					# check if from neighbouring chunk
					var chunk: Vector3i = Vector3i(0,0,0)
					var c: int = 0 #min x, y or z
					var max: int = gridSize+1 # Max x, y or z
					if (x==max): chunk.x += 1
					elif (x==0): chunk.x -= 1
					if (y==max): chunk.y += 1
					elif (y==0): chunk.y -= 1
					if (z==max): chunk.z += 1
					elif (z==0): chunk.z -= 1
					if (chunk):
						
						pass
					else:
						# get value from other chunk
						pass
					
		generated = true
