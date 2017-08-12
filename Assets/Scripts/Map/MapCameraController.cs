using UnityEngine;
using System.Collections;

public class MapCameraController : MonoBehaviour
{
    public float CameraZoomScale = 0.1f;
    public float CameraTranslateScale = 0.1f;
    public float CameraRotateScale = 5f;

    Camera cam;
    bool isCameraOrtho;
    bool isMouseMiddleDown = false;
    bool isRotateR = false;
    bool isRotateL = false;
    bool isRotateUp = false;
    bool isRotateDown = false;
    bool isMoveForward = false;
    bool isMoveBack = false;
    bool isMoveLeft = false;
    bool isMoveRight = false;
    bool isMoveUp = false;
    bool isMoveDown = false;
    Vector3 mouseLastPosition;

	// Use this for initialization
	void Start ()
    {
        cam = Camera.main;
        isCameraOrtho = cam.orthographic;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetMouseButtonDown(2))
        {
            isMouseMiddleDown = true;
            mouseLastPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2))
        {
            isMouseMiddleDown = false;
        }

        if (isMouseMiddleDown && mouseLastPosition != Input.mousePosition)
        {
            translateCameraPosition(Input.mousePosition - mouseLastPosition);
            mouseLastPosition = Input.mousePosition;
        }

        keyEvent();

        if (isRotateL)
        {
            rotateCameraAroundYaxis(CameraRotateScale);
        }

        if (isRotateR)
        {
            rotateCameraAroundYaxis(-CameraRotateScale);
        }

        if (isRotateUp)
        {
            rotateCameraAroundXaxis(-CameraRotateScale);
        }

        if (isRotateDown)
        {
            rotateCameraAroundXaxis(CameraRotateScale);
        }

        if (isMoveForward)
        {
            cam.transform.transform.Translate(Vector3.forward * CameraTranslateScale);
        }

        if (isMoveBack)
        {
            cam.transform.transform.Translate(Vector3.back * CameraTranslateScale);
        }

        if (isMoveLeft)
        {
            cam.transform.transform.Translate(Vector3.left * CameraTranslateScale);
        }

        if (isMoveRight)
        {
            cam.transform.transform.Translate(Vector3.right * CameraTranslateScale);
        }

        if (isMoveUp)
        {
            translateCameraPosition(new Vector3(0f, 0.3f, 0f));
        }

        if (isMoveDown)
        {
            translateCameraPosition(new Vector3(0f, -0.3f, 0f));
        }

        float scrollY = Input.GetAxis("Mouse ScrollWheel");

        if (Input.mousePosition.x < 0 || Input.mousePosition.y < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.y > Screen.height)
        {
            scrollY = 0f;
        }

        if (scrollY != 0f)
        {
            StartCoroutine(zoomCamera(scrollY));
        }
    }

    void translateCameraPosition(Vector3 diff)
    {
        cam.transform.position += diff * CameraTranslateScale;
    }

    IEnumerator zoomCamera(float diff)
    {
        int counter = 0;
        int stopCounter = 3;
        Vector3 scroll = diff > 0 ? cam.transform.forward * -CameraZoomScale : cam.transform.forward * CameraZoomScale;

        while (counter++ < stopCounter)
        {
            if (isCameraOrtho)
            {
                cam.orthographicSize += scroll.y;
            }
            else
            {
                cam.transform.position += scroll;
            }
            yield return null;
        }
    }

    void rotateCameraAroundYaxis(float angle)
    {
        cam.gameObject.transform.RotateAround(cam.gameObject.transform.position, Vector3.up, angle * Time.deltaTime);
    }

    void rotateCameraAroundXaxis(float angle)
    {
        cam.gameObject.transform.RotateAround(cam.gameObject.transform.position, cam.transform.right, angle * Time.deltaTime);
    }

    void keyEvent(KeyCode code, ref bool val)
    {
        if (Input.GetKeyDown(code))
        {
            val = true;
        }
        else if (Input.GetKeyUp(code))
        {
            val = false;
        }
    }

    void keyEvent()
    {
        keyEvent(KeyCode.Q, ref isRotateR);
        keyEvent(KeyCode.E, ref isRotateL);
        keyEvent(KeyCode.R, ref isRotateUp);
        keyEvent(KeyCode.F, ref isRotateDown);
        keyEvent(KeyCode.W, ref isMoveForward);
        keyEvent(KeyCode.UpArrow, ref isMoveForward);
        keyEvent(KeyCode.A, ref isMoveLeft);
        keyEvent(KeyCode.LeftArrow, ref isMoveLeft);
        keyEvent(KeyCode.S, ref isMoveBack);
        keyEvent(KeyCode.DownArrow, ref isMoveBack);
        keyEvent(KeyCode.D, ref isMoveRight);
        keyEvent(KeyCode.RightArrow, ref isMoveRight);
        keyEvent(KeyCode.X, ref isMoveDown);
        keyEvent(KeyCode.PageDown, ref isMoveDown);
        keyEvent(KeyCode.C, ref isMoveUp);
        keyEvent(KeyCode.PageUp, ref isMoveUp);
    }
}
