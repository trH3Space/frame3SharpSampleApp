using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
	public static CameraController instance;

	public float rotateSensitivity = 0.3f;
	public float scaleFactor = 0.2f;
	public float animationTime = 1.0f;
	public float manipulateRangeLimit = 100f;
	public float minZoomDistance = 0.1f;

	public bool IsMoving => _shifting || _rotating;

	Camera _camera;
	bool _shifting;
	bool _rotating;

	Vector3 _click;
	Vector3 _rotationPoint;
	Vector3 _screenClick;
	Vector3 _objectClick;

	// animation
	Quaternion _srcRotation;
	Quaternion _dstRotation;
	Vector3 _srcPosition;
	Vector3 _dstPosition;
	float _animationPhase = 1.0f;

	public void Start()
	{
		instance = this;
	}

	private void Awake()
	{
		_camera = GetComponent<Camera>();
	}

	void Update()
	{
		if(_animationPhase < 1.0f)
		{
			_animationPhase += Time.deltaTime * 1.0f / animationTime;
			_animationPhase = Mathf.Clamp01(_animationPhase);
			_camera.transform.rotation = Quaternion.Lerp(_srcRotation, _dstRotation, _animationPhase);
			_camera.transform.position = Vector3.Lerp(_srcPosition, _dstPosition, _animationPhase);
			return;
		}
		
		// shift camera
		if(Input.GetKeyDown(KeyCode.Mouse2))
		{
			_shifting = true;
			_screenClick = Input.mousePosition;
			_objectClick = MousePosByWorldPlaneOrObject;
			_click = MousePosByVerticalObjectPlane(_objectClick);
		}
		
		if(Input.GetKeyUp(KeyCode.Mouse2))
		{
			_shifting = false;
		}
		
		// rotate camera
		if(Input.GetKeyDown(KeyCode.Mouse1))
		{
			_rotating = true;
			_screenClick = Input.mousePosition;
			_rotationPoint = MousePosByWorldPlaneOrObject;
		}
		
		if(Input.GetKeyUp(KeyCode.Mouse1))
		{
			_rotating = false;
		}

		// Apply shift, rotate and scroll
		
		// shift
		if(_shifting)
		{
			var pos = MousePosByVerticalObjectPlane(_objectClick);
			var delta = pos - _click;
			_camera.transform.position -= delta;
			_click = MousePosByVerticalObjectPlane(_objectClick);
		}
		
		// rotate
		if(_rotating)
		{
			var screenDelta = (Input.mousePosition - _screenClick) * rotateSensitivity;
			var delta = -screenDelta.magnitude;
			var tf = _camera.transform;
			_camera.transform.RotateAround(_rotationPoint, Vector3.up, screenDelta.x);
			_camera.transform.RotateAround(_rotationPoint, tf.right, -screenDelta.y);

			_screenClick = Input.mousePosition;
		}
		
		// scroll
		var scroll = Input.mouseScrollDelta.y;

		// perform scrolling only when mouse outside UI
		if(!IsMouseOnUI() && scroll != 0f)
		{
			var dst = MousePosByWorldPlaneOrObject;
			var src = _camera.transform.position;
			
			// we should use this computation for determenism - 
			// if we are scrolling forth and back, we should return the same position
			if(scroll < 0f)
			{
				_camera.transform.position = (src - dst * scaleFactor) / (1f - scaleFactor);
			} 
			else
			{
				_camera.transform.position = src + (dst - src) * scaleFactor;
			}
			
			// apply minZoomDistance to restrict scrolling too close to objects
			var dir = dst - _camera.transform.position;
			if (dir.magnitude < minZoomDistance)
			{
				_camera.transform.position = dst - dir.normalized * minZoomDistance;
			}
		}

	}

	void OnDrawGizmos()
	{
		// to work in editor
		if(_camera == null) return;

		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(MousePosByWorldPlaneOrObject, 0.1f);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(MousePosByWorldHorizOrVertPlane, 0.1f);
	}

	public void AnimateTo(Transform transform)
	{
		_srcRotation = _camera.transform.rotation;
		_dstRotation = transform.rotation;
		_srcPosition = _camera.transform.position;
		_dstPosition = transform.position;
		_animationPhase = 0.0f;
	}

	public Ray SelectionRay => _camera.ScreenPointToRay(Input.mousePosition);

	public Vector3 MousePosByVerticalObjectPlane(Vector3 objectPos)
	{
		var ray = SelectionRay;
		if(RaycastPlane(ray, Camera.main.transform.forward, objectPos, out var intersection))
		{
			return intersection;
		}
		return ray.origin;
	}

	bool RaycastPlane(Ray ray, Vector3 n, Vector3 o, out Vector3 isec)
	{
		var plane = new Plane(n, o);
		plane.Raycast(ray, out var cast);
		if(cast <= 0f) {
			isec = ray.origin;
			return false;
		}

		isec = ray.GetPoint(cast);
		return true;
	}

	public Vector3 MousePosByWorldHorizOrVertPlane
	{
		get
		{
			var ray = SelectionRay;

			var hPlaneCasted = RaycastPlane(ray, Vector3.up, Vector3.zero, out var hPlanePoint);

			var cameraDir = _camera.transform.forward;
			cameraDir.y = 0f;
			cameraDir.Normalize();
			var rangePlanePos = _camera.transform.position;
			rangePlanePos.y = 0f;
			rangePlanePos += cameraDir * manipulateRangeLimit;

			var vPlaneCasted = RaycastPlane(ray, _camera.transform.forward, rangePlanePos, out var vPlanePoint);

			if(hPlaneCasted && vPlaneCasted)
			{
				if((ray.origin - hPlanePoint).sqrMagnitude < (ray.origin - vPlanePoint).sqrMagnitude)
				{
					return hPlanePoint;
				}
				return vPlanePoint;
			}

			if(vPlaneCasted)
			{
				return vPlanePoint;
			}

			return hPlanePoint;
		}
	}

	public Vector3 MousePosByWorldPlaneOrObject
	{
		get
		{
			var ray = SelectionRay;
			if(Physics.Raycast(ray, out var hit))
			{
				return hit.point;
			}

			return MousePosByWorldHorizOrVertPlane;
		}
	}

	public bool IsMouseOnUI()
	{
		return EventSystem.current.IsPointerOverGameObject();
	}

}
