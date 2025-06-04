using UnityEngine;
using UnityEngine.AI;

public class FloorPlacerObject : MonoBehaviour
{
	[Header("기본 설정")]
	public Camera mainCamera;
	public Transform player;
	public Transform worldSpaceParent;
	public GameObject objectPreviewPrefab; // 설치할 설치형 오브젝트 프리팹
	public LayerMask installableLayer;
	public LayerMask blockLayerMask;
	public float maxPlaceDistance = 3f;
	public float stopDistance = 1.5f;
	public NavMeshSurface navMeshSurface;

	private GameObject currentPreview;
	private Renderer previewRenderer;
	private Vector3 targetPosition;
	private bool isMovingToInstallPoint = false;
	private Vector3 destinationQueued;

	private const float positionOffset = 0.001f;
	private Color baseColor;

	void Start()
	{
		if (mainCamera == null) mainCamera = Camera.main;
		if (worldSpaceParent == null) { Debug.LogError("WorldSpace 부모를 할당해주세요."); return; }

		currentPreview = Instantiate(objectPreviewPrefab, worldSpaceParent);
		currentPreview.transform.localRotation = Quaternion.identity;
		currentPreview.transform.localPosition = Vector3.zero;

		previewRenderer = currentPreview.GetComponent<Renderer>();
		if (previewRenderer == null) Debug.LogError("Renderer가 없습니다.");
		else baseColor = previewRenderer.material.color;

		Collider previewCollider = currentPreview.GetComponent<Collider>();
		if (previewCollider != null) previewCollider.isTrigger = true;
	}

	void Update()
	{
		if (isMovingToInstallPoint) return;

		Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, 100f, installableLayer))
		{
			Vector3 snappedPos = SnapToGrid(hit.point);
			targetPosition = snappedPos;
			snappedPos += new Vector3(positionOffset, positionOffset, -positionOffset);
			currentPreview.transform.localPosition = snappedPos;
			currentPreview.SetActive(true);

			if (IsPlaceable(snappedPos))
			{
				previewRenderer.material.color = Color.green;

				if (Input.GetMouseButtonDown(0))
				{
					MoveToInstall(snappedPos);
				}
			}
			else
			{
				previewRenderer.material.color = Color.red;

				if (Input.GetMouseButtonDown(0))
				{
					Debug.Log("설치 불가능");
				}
			}
		}
		else
		{
			currentPreview.SetActive(false);
			previewRenderer.material.color = baseColor;
		}
	}

	bool IsPlaceable(Vector3 snappedPos)
	{
		Vector3 worldPos = worldSpaceParent.TransformPoint(snappedPos);
		Vector3 size = GetObjectBounds(currentPreview);

		Collider[] overlaps = Physics.OverlapBox(worldPos, size / 2f, Quaternion.identity, blockLayerMask, QueryTriggerInteraction.Ignore);
		if (overlaps.Length > 0) return false;

		float distance = Vector3.Distance(player.position, worldPos);
		return distance <= maxPlaceDistance;
	}

	void MoveToInstall(Vector3 snappedPos)
	{
		destinationQueued = snappedPos;
		isMovingToInstallPoint = true;

		Vector3 worldTarget = worldSpaceParent.TransformPoint(snappedPos);
		Vector3 direction = (worldTarget - player.position).normalized;
		Vector3 stopBeforeTarget = worldTarget - direction * stopDistance;

		NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
		if (agent != null && agent.isOnNavMesh)
		{
			agent.SetDestination(stopBeforeTarget);
		}

		Invoke(nameof(InstallTile), 0.5f); // 간단한 설치용 딜레이
	}

	void InstallTile()
	{
		GameObject obj = Instantiate(objectPreviewPrefab, worldSpaceParent);
		obj.transform.localPosition = destinationQueued;
		obj.transform.localRotation = Quaternion.identity;

		Collider c = obj.GetComponent<Collider>();
		if (c != null) c.isTrigger = false;

		Renderer r = obj.GetComponent<Renderer>();
		if (r != null) r.material.color = baseColor;

		if (navMeshSurface != null) navMeshSurface.BuildNavMesh();

		isMovingToInstallPoint = false;
		Debug.Log("설치 완료");
	}

	Vector3 SnapToGrid(Vector3 worldPos)
	{
		float cellSize = 1f;
		Vector3 localPos = worldSpaceParent.InverseTransformPoint(worldPos);
		int x = Mathf.RoundToInt(localPos.x / cellSize);
		int z = Mathf.RoundToInt(localPos.z / cellSize);
		return new Vector3(x * cellSize, 0f, z * cellSize);
	}

	Vector3 GetObjectBounds(GameObject obj)
	{
		Collider col = obj.GetComponent<Collider>();
		return col ? col.bounds.size : Vector3.one;
	}
}
