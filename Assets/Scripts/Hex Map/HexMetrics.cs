using UnityEngine;


public static class HexMetrics
{


	public const float OUTER_TO_INNER = 0.866025404f;
	public const float INNER_TO_OUTER = 1f / OUTER_TO_INNER;

	public const float OUTER_RADIUS = 10f;
	public const float INNER_RADIUS = OUTER_RADIUS * OUTER_TO_INNER;
	public const float INNER_DIAMETER = INNER_RADIUS * 2.0f;
	
	public const float SOLID_FACTOR = 0.8f;
	public const float BLEND_FACTOR = 1.0f - SOLID_FACTOR;
	public const float WATER_FACTOR = 0.6f;
	public const float WATER_BLEND_FACTOR = 1.0f - WATER_FACTOR;

	public const float ELEVATION_STEP = 3.0f;

	public const int TERRACES_PER_SLOPE = 2;
	public const int TERRACE_STEP = TERRACES_PER_SLOPE * 2 + 1;
	public const float HORIZONTAL_TERRACE_STEP_SIZE = 1.0f / TERRACE_STEP;
	public const float VERTICAL_TERRACE_STEP_SIZE = 1.0f / (TERRACES_PER_SLOPE + 1);

	public const float CELL_PERTURB_STRENGTH = 4.0f;//4
	public const float ELEVATION_PERTURB_STRENGTH = 1.5f;//1.5
	public const float NOISE_SCALE = 0.003f;

	public const int CHUNK_SIZE_X = 5;
	public const int CHUNK_SIZE_Z = 5;

	public const float STREAM_BED_ELEVATION_OFFSET = -1.75f;
	public const float WATER_ELEVATION_OFFSET = -0.5f;
	
	public const int HASH_GRID_SIZE = 256;
	public const float HASH_GRID_SCALE = 0.25f;

	public const float WALL_ELEVATION_OFFSET = VERTICAL_TERRACE_STEP_SIZE;
	public const float WALL_TOWER_THRESHOLD = 0.5f;
	public const float WALL_THICKNESS = 0.75f;
	public const float WALL_Y_OFFSET = -1f;
	public const float WALL_HEIGHT = 4f;

	public const float BRIDGE_DESIGN_LENGTH = 7f;



	public static Vector3[] corners = {
		new Vector3(0f, 0f, OUTER_RADIUS),
		new Vector3(INNER_RADIUS, 0f, 0.5f * OUTER_RADIUS),
		new Vector3(INNER_RADIUS, 0f, -0.5f * OUTER_RADIUS),
		new Vector3(0f, 0f, -OUTER_RADIUS),
		new Vector3(-INNER_RADIUS, 0f, -0.5f * OUTER_RADIUS),
		new Vector3(-INNER_RADIUS, 0f, 0.5f * OUTER_RADIUS),
		new Vector3(0f, 0f, OUTER_RADIUS)
	};

	public static int wrapSize;
	public static Texture2D noiseSource;
	public static HexHash[] hashGrid;
	public static float[][] featureThresholds = {
		new [] {0.0f, 0.0f, 0.4f},
		new [] {0.0f, 0.4f, 0.6f},
		new [] {0.4f, 0.6f, 0.8f}
	};


	public static bool wrapping =>
		wrapSize > 0;


	public static float[] GetFeatureThresholds(int level) => 
		featureThresholds[level];


	public static Vector3 GetFirstCorner(HexDirection direction) => 
		corners[(int) direction];
	
	
	public static Vector3 GetSecondCorner(HexDirection direction) => 
		corners[(int)direction + 1];


	public static Vector3 GetFirstSolidCorner(HexDirection direction) => 
		corners[(int)direction] * SOLID_FACTOR;


	public static Vector3 GetSecondSolidCorner(HexDirection direction) => 
		corners[(int)direction + 1] * SOLID_FACTOR;


	public static Vector3 GetFirstWaterCorner(HexDirection direction) =>
		corners[(int)direction] * WATER_FACTOR;


	public static Vector3 GetSecondWaterCorner(HexDirection direction) =>
		corners[(int)direction + 1] * WATER_FACTOR;


	public static Vector3 GetBridge(HexDirection direction) =>
		(corners[(int) direction] + corners[(int) direction + 1]) *  BLEND_FACTOR;


	public static Vector3 GetWaterBridge(HexDirection direction) =>
		(corners[(int)direction] + corners[(int)direction + 1]) * WATER_BLEND_FACTOR;


	public static Vector3 GetSolidEdgeMiddle(HexDirection direction) =>
		(corners[(int) direction] + corners[(int) direction + 1]) * (0.5f * SOLID_FACTOR);



	public static void InitializeHashGrid(int seed)
	{
		hashGrid = new HexHash[HASH_GRID_SIZE * HASH_GRID_SIZE];
		
		var currentState = Random.state;
		Random.InitState(seed);
		
		for (var i = 0; i < hashGrid.Length; i++)
		{
			hashGrid[i] = HexHash.Create();
		}

		Random.state = currentState;
	}


	public static HexHash SampleHashGrid(Vector3 position)
	{
		var x = (int) (position.x * HASH_GRID_SCALE) % HASH_GRID_SIZE;
		if (x < 0)
		{
			x += HASH_GRID_SIZE;
		}
		var z = (int) (position.z * HASH_GRID_SCALE) % HASH_GRID_SIZE;
		if (z < 0)
		{
			z += HASH_GRID_SIZE;
		}
		return hashGrid[x + z * HASH_GRID_SIZE];
	}


	public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
	{
		var h = step * HORIZONTAL_TERRACE_STEP_SIZE;
		a.x += (b.x - a.x) * h;
		a.z += (b.z - a.z) * h;
		// ReSharper disable once PossibleLossOfFraction
		var v = ((step + 1) / 2) * VERTICAL_TERRACE_STEP_SIZE;
		a.y += (b.y - a.y) * v;
		return a;
	}


	public static Color TerraceLerp(Color a, Color b, int step)
	{
		var h = step * HORIZONTAL_TERRACE_STEP_SIZE;
		return Color.Lerp(a, b, h);
	}
	

	public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
	{
		if (elevation1 == elevation2)
		{
			return HexEdgeType.Flat;
		}

		var delta = elevation2 - elevation1;
		if (delta == 1 || delta == -1)
		{
			return HexEdgeType.Slope;
		}

		return HexEdgeType.Cliff;
	}


	public static Vector4 SampleNoise(Vector3 position)
	{
		var sample =  noiseSource.GetPixelBilinear(position.x * NOISE_SCALE, position.z * NOISE_SCALE);

		if (wrapping && position.x < INNER_DIAMETER * 1.5f)
		{
			var sample2 = noiseSource.GetPixelBilinear(
				(position.x + wrapSize * INNER_DIAMETER) * NOISE_SCALE,
				position.z * NOISE_SCALE);

			sample = Vector4.Lerp(sample2, sample, position.x * (1f / INNER_DIAMETER) - 0.5f);
		}

		return sample;
	}


	public static Vector3 Perturb(Vector3 position)
	{
		var sample = SampleNoise(position);
		position.x += (sample.x * 2.0f - 1.0f) * CELL_PERTURB_STRENGTH;
		position.z += (sample.z * 2.0f - 1.0f) * CELL_PERTURB_STRENGTH;
		return position;
	}


	public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
	{
		Vector3 offset;
		offset.x = far.x - near.x;
		offset.y = 0f;
		offset.z = far.z - near.z;
		return offset.normalized * (HexMetrics.WALL_THICKNESS * 0.5f);
	}


	public static Vector3 WallLerp(Vector3 near, Vector3 far)
	{
		near.x += (far.x - near.x) * 0.5f;
		near.z += (far.z - near.z) * 0.5f;
		var v = near.y < far.y ? WALL_ELEVATION_OFFSET : (1f - WALL_ELEVATION_OFFSET);
		near.y += (far.y - near.y) * v + WALL_Y_OFFSET;
		return near;
	}

}