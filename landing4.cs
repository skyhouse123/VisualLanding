/*
 * Filename: landing2.cs
 * Author: Huatian Wang & Qinbing Fu 
 * Date: June 2020
 */

/// image processing branches control: comment the unused modes, leave only one working for the programme
//#define IMAGE_PROCESS_TRUNCATE
#define IMAGE_PROCESS_DOWNSAMPLE
//#define IMAGE_PROCESS_ADJUST

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// This class implements the honeybee angular velocity detectors and simulates tunnel crossing behaviour.
/// </summary>
public class landing4 : MonoBehaviour
{
    // global frames count
    public static int globalFrames;
    // cameras in Unity for switch (press 1, 2, 3 when gaming)
    public GameObject[] cameras;
    // walls
    public GameObject[] walls;
    // top-down scene camera
    public Camera mainCamera;
    // left camera with bee
    public Camera beeVentralEye;
    // right camera with bee
    //public Camera beeRightEye;
    //public Texture2D leftTex;
    //public Texture2D rightTex;
    // camera pixel width
    private int c_width;
    // camera pixel height
    private int c_height;
    // downsampled image width
    private int d_width;
    // downsampled image height
    private int d_height;
    // x-axis movement limit
    private float limit_x;
    // z-axis movement limit
    private float limit_z;
    private int shot_Number;
    public string[] shotcuts;
    // if monitor elapsed time in Update function
    private static bool _watch = false;
    private static bool _changeAudioListener = true;
    // if allow manual control of bee
    private static bool _allowManualControl = false;
    // if enable switch cameras
    private static bool _enableSwitchCameras = true;
    // if allow screen shot from the scene camera
    private static bool _allowScreenShot = false;
    // if debug to print something in Update function
    private static bool _debug = false;
    // left captured pattern in the defined time window (10)
    private static float[,,] ventral_pattern;
    // right captured pattern in the defined time window (10)
    //private static float[,,] right_pattern;
    // left resized pattern in the defined time window (10)
    private static float[,,] ventral_resized_pattern;
    // right resized pattern in the defined time window (10)
    //private static float[,,] right_resized_pattern;
    // initial forward speed of bee
    public float speed;
    private float inito;
    // translation offset
    private static float z_offset;
    // initial start position of bee along z-axis
    private static float y_start_position;
    //private System.Diagnostics.TraceListener listener;
    // a bee object from the sub-class
    public HoneyBee bee;
    public float presetVR;


    // Use this for initialization
    void Start()
    {
        globalFrames = 0;

        //SetBoundary();
        //BindVision();

#if IMAGE_PROCESS_TRUNCATE
        c_width = 66;
        c_height = 60;
        bee = new HoneyBee(c_width, c_height);
        left_pattern = new float[c_height, c_width, bee.time_length];
        right_pattern = new float[c_height, c_width, bee.time_length];
#endif

#if IMAGE_PROCESS_DOWNSAMPLE
        c_width = beeVentralEye.pixelWidth;
        c_height = beeVentralEye.pixelHeight;
        d_width = 66;
        d_height = 66;
        bee = new HoneyBee(d_width, d_height);
        ventral_pattern = new float[c_height, c_width, bee.time_length];
        //right_pattern = new float[c_height, c_width, bee.time_length];
        ventral_resized_pattern = new float[d_height, d_width, bee.time_length];
        //right_resized_pattern = new float[d_height, d_width, bee.time_length];
#endif

#if IMAGE_PROCESS_ADJUST
        c_width = 66;
        c_height = 60;
        // change left camera rendering
        ChangeCameraSize(c_width, c_height, ref beeLeftEye);
        // change right camera rendering
        ChangeCameraSize(c_width, c_height, ref beeRightEye);
        bee = new HoneyBee(c_width, c_height);
        left_pattern = new float[c_height, c_width, bee.time_length];
        right_pattern = new float[c_height, c_width, bee.time_length];
#endif
        float ws1 = Random.Range(-0.3f, 0.3f);
        float ws2 = Random.Range(-0.3f, 0.3f);

        //speed = 14f;
        //speed = 15f + ws1; //0.38
        //z_offset = 0.05f;
        //y_start_position = 15.0f + 0 * ws2;

        // initialise bee start position
        //this.transform.position = new Vector3(this.transform.position.x, y_start_position, this.transform.position.z);
        inito = speed / y_start_position;
    }

    // Update is called once per frame
    void Update()
    {
        var watch = new System.Diagnostics.Stopwatch();
        if (_watch)
        {
            watch.Start();
        }

        // this marks the centre line.
        //DrawLine(new Vector3(-45, 0, 0), new Vector3(45, 0, 0), new Color(1.0f, 0, 0));

        //print(globalFrames);

        if (_allowManualControl)
            this.ControlFromKeyboard(speed);

        if (_enableSwitchCameras)
            this.ProcSwitchCamera();

            if (_allowScreenShot)
                this.StartCoroutine(UploadPNG());

        //MoveADistance(init_speed, limit_x);
        //TestMove();

        ReadFromCamera(beeVentralEye, ventral_pattern, bee.time_length);

#if IMAGE_PROCESS_DOWNSAMPLE
        // downsampling left image
        ResizeImage(ventral_pattern, ref ventral_resized_pattern, c_width, c_height, d_width, d_height, bee.time_length);
        // downsampling right image
        // ResizeImage(right_pattern, ref right_resized_pattern, c_width, c_height, d_width, d_height, bee.time_length);
        bee.AngularVelocityEstimator(ventral_resized_pattern, globalFrames + 1);
#else
        bee.AngularVelocityEstimator(left_pattern, right_pattern, globalFrames + 1);
#endif
        float omega = speed / this.transform.position.y;
        //float para = 0.11f * Mathf.Exp(-globalFrames * 0.2f);
        float tempsum = 0;
        float para = 0.04f;
        float k0 = 0.5f; float k2 = 0.5f;
        float gain = -1.5f * Mathf.Log(globalFrames) * 0.17f ;
        para = k0 * Mathf.Exp(gain) + k2 * Mathf.Exp(gain) * tempsum ;
        if (globalFrames <= 24)
        {
            FlatMove(speed);
            if (globalFrames > 21)
            {
                presetVR += bee.ventral_response / 3;
            }
        }
        if (globalFrames > 24)
        {
            if (this.transform.position.y > 0.5f)
            {
                float temp = presetVR - bee.ventral_response;

                float limit = speed*(1 + 0.1f*bee.ventral_response * (presetVR - bee.ventral_response));
                speed += bandp(limit - speed);


                //speed *= 1 + para * (inito - omega);
                //speed *= 1 + 0.05f * (presetVR-bee.ventral_response );
                //speed *= 1 + para * (presetVR - bee.ventral_response);



                //float limit = speed * para * temp;
               // speed += bandp(limit - speed);


                
                tempsum += temp;
                SelfMove(speed);
            }
            else {
                SelfMove(0);
            }
            
        }
        print(System.String.Format( "omega: {0:F}", omega));
        print(System.String.Format("ventral_response: {0:F}", bee.ventral_response));
        //StartCoroutine(UploadPNG());

        // string result1 = @"F:\flatlandingSpeed_new3.txt";
        //string result1 = @"C:\Users\skyho\Desktop\temp\landingSpeed.txt";
       // FileStream fs = new FileStream(result1, FileMode.Append);
       // StreamWriter wr = null;
      //  wr = new StreamWriter(fs);
      //  wr.WriteLine(speed + "\n");
      //  wr.Close();

       // string result2 = @"F:\flatlandingVR_new3.txt";
        //string result2 = @"C:\Users\skyho\Desktop\temp\landingVR.txt";
      //  FileStream fs2 = new FileStream(result2, FileMode.Append);
      //  StreamWriter wr2 = null;
     //   wr2 = new StreamWriter(fs2);
      //  wr2.WriteLine(bee.ventral_response + "\n");
      //  wr2.Close();

      //  string result3 = @"F:\flatlandingx_new3.txt";
        //string result3 = @"C:\Users\skyho\Desktop\temp\landingx.txt";
     //   FileStream fs3 = new FileStream(result3, FileMode.Append);
      //  StreamWriter wr3 = null;
     //   wr3 = new StreamWriter(fs3);
       // wr3.WriteLine(this.transform.position.x );
      //  wr3.Close();

       // string result4 = @"F:\flatlandingy_new3.txt";
        //string result4 = @"C:\Users\skyho\Desktop\temp\landingy.txt";
      //   FileStream fs4 = new FileStream(result4, FileMode.Append);
     //   StreamWriter wr4 = null;
    //    wr4 = new StreamWriter(fs4);
    //    wr4.WriteLine(this.transform.position.y );
    //    wr4.Close();
//

        if (_debug)
        {
            print(System.String.Format("pattern: {0:F} {1:F} ", ventral_pattern[100, 100, globalFrames % bee.time_length], ventral_pattern[200, 200, (globalFrames + 1) % bee.time_length]));
            //print(System.String.Format("lambda: {0:F} {1:F}", bee.left_lambdapre, bee.right_lambdapre));
            //print(System.String.Format("mean response: {0:F} {1:F}", bee.meanL, bee.meanR));
            //print(System.String.Format("estimator output: {0:F} {1:F}", bee.left_response, bee.right_response));
        }

        //System.Threading.Thread.Sleep(10);

        globalFrames++;

        if (_watch)
        {
            watch.Stop();
            int elapsed = (int)watch.ElapsedMilliseconds;
            print(System.String.Format("Loop processed in {0} ms", elapsed));
        }

    }



    float bandp(float temp)
    {
        if (Mathf.Abs(temp) > 0.2)
            return Mathf.Sign(temp)*0.2f;
        else
            return temp;
        
    }
    /// <summary>
    /// Change camera rendering resolution
    /// </summary>
    /// <param name="wanted_width"></param>
    /// <param name="wanted_height"></param>
    /// <param name="camera"></param>
    void ChangeCameraSize(int input_width, int input_height, ref Camera camera)
    {
        float ratio_width = (float)input_width / (float)camera.pixelWidth;
        float ratio_height = (float)input_height / (float)camera.pixelHeight;
        camera.rect = new Rect(0, 0, ratio_width, ratio_height);
    }

    /// <summary>
    /// Resize images (downsampling)
    /// </summary>
    /// <param name="inputMat"></param>
    /// <param name="outputMat"></param>
    /// <param name="in_width"></param>
    /// <param name="in_height"></param>
    /// <param name="out_width"></param>
    /// <param name="out_height"></param>
    /// <param name="time_window"></param>
    void ResizeImage(float[,,] inputMat, ref float[,,] outputMat, int in_width, int in_height, int out_width, int out_height, int time_window)
    {
        int cur_t = globalFrames % time_window;
        int w_step = in_width / out_width + 1;
        int h_step = in_height / out_height + 1;
        int num = w_step * h_step;
        float mean;
        for (int i = 0; i < in_height - h_step; i = i + h_step)
        {
            for (int j = 0; j < in_width - w_step; j = j + w_step)
            {
                mean = 0;
                for (int ii = i; ii < i + h_step; ii++)
                {
                    for (int jj = j; jj < j + w_step; jj++)
                    {
                        mean += inputMat[ii, jj, cur_t];
                    }
                }
                outputMat[i / h_step, j / w_step, cur_t] = mean / num;
            }
        }
    }

    /// <summary>
    /// Retrieve light intensities from two-side cameras
    /// </summary>
    /// <param name="ventral_camera"></param>
    /// <param name="ventral_image"></param>  
    /// <param name="time_window"></param>
    void ReadFromCamera(Camera ventral_camera, float[,,] ventral_image, int time_window)
    {
        RenderTexture rt = new RenderTexture(c_width, c_height, -1);
        ventral_camera.targetTexture = rt;
        ventral_camera.Render();
        RenderTexture.active = rt;
        Texture2D screenShotLeft = new Texture2D(c_width, c_height, TextureFormat.RGB24, false);
        screenShotLeft.ReadPixels(new Rect(0, 0, c_width, c_height), 0, 0);
        screenShotLeft.Apply();

        for (int y = 1; y < c_height - 1; y++)
        {
            for (int x = 1; x < c_width - 1; x++)
            {
                Color c = screenShotLeft.GetPixel(x, y);
                ventral_image[y, x, globalFrames % time_window] = c.grayscale;
            }
        }
        RenderTexture.active = null;
        ventral_camera.targetTexture = null;
        Destroy(screenShotLeft);

        GameObject.Destroy(rt);
    }

    /// <summary>
    /// This is only for debugging movements.
    /// </summary>
    void TestMove()
    {
        float x = 0;
        float y = 0;
        float z = 0;
        if (this.transform.position.y < 20)
            this.transform.Translate(x, y, z);
    }

    /// <summary>
    /// Control movements from keyboard
    /// </summary>
    /// <param name="speed"></param>
    void ControlFromKeyboard(float speed)
    {
        float x = Input.GetAxis("Horizontal") * Time.deltaTime * speed;
        float z = Input.GetAxis("Vertical") * Time.deltaTime * speed;
        Move(x, z);
        print(System.String.Format("{0} {1}", transform.position.x, transform.position.z));
    }

    /// <summary>
    /// Navigate to a goal distance
    /// </summary>
    /// <param name="speed"></param>
    /// <param name="x_position"></param>
    void MoveADistance(float speed, float x_position)
    {
        float ws = Random.Range(0.1f, 0.1f);
        float x = Time.deltaTime * speed * ws;
        //float y = Time.deltaTime * speed;
        float z = Time.deltaTime * speed;
        if (this.transform.position.x < x_position)
        {
            Move(x, z);
            print(System.String.Format("{0} {1}", transform.position.x, transform.position.z));
        }
        else
            print("stop moving");
    }

    /// <summary>
    /// Move along x and z axes directions
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    void Move(float x, float y)
    {
        this.transform.Translate(0, y, x);
        //BindVision();
    }

    /// <summary>
    /// Bind binocular vision (two-side cameras) to this bee object at every frames
    /// </summary>
    void BindVision()
    {
        beeVentralEye.transform.SetPositionAndRotation(new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z), new Quaternion(30, 90, 0, 0));
        //beeRightEye.transform.SetPositionAndRotation(new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z), new Quaternion(0, 180, 0, 0));
    }

    /// <summary>
    /// Set boudaries to movements
    /// </summary>
    void SetBoundary()
    {
        limit_x = walls[0].transform.localScale.x / 2 - this.transform.localScale.x;
        limit_z = Mathf.Abs(walls[0].transform.position.z) - walls[0].transform.localScale.z;
    }

    /// <summary>
    /// This function implements autonomous navigation of bee crossing tunnel.
    /// </summary>
    /// <param name="lres"></param>
    /// <param name="rres"></param>
    void SelfMove(float speed)
    {
  
        float tanbeta = 0.5f;
        //float ws = Random.Range(0.1f, 0.1f);
        //float forward_move = Time.deltaTime * init_speed;
        // compare model response from two sides
        float forward_move = Time.deltaTime * speed;
        //forward_move = 0.002f*(System.Math.Abs(lres) + System.Math.Abs(rres));
        //forward_move = 0.5f;

        float descent_move = -Time.deltaTime * tanbeta * speed;
        // boundary check
        //if (this.transform.position.x >= limit_x)
        // Move(0, 0);
        //else if (Mathf.Abs(this.transform.position.z) >= limit_z)
        // Move(0, forward_move);
        //else
        Move(forward_move, descent_move);
    }

    void FlatMove(float speed)
    {
        float forward_move = Time.deltaTime * speed;
        float descent_move = 0;
        Move(forward_move, descent_move);

    }
    /// <summary>
    /// Coroutine to save screen images
    /// </summary>
    /// <returns></returns>
    IEnumerator UploadPNG()
    {
        shot_Number++;
        yield return new WaitForEndOfFrame();
        int width = Screen.width;
        int height = Screen.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex);
        string filepath = string.Format("{0:D4}{1:D2}.png", "/tempshot/", shot_Number.ToString());
        string dataPath = "F:/LandingLGMDShot/";
        //string dataPath = "C:/Users/skyho/Desktop/temp/";
        string filename =  dataPath+filepath;
        File.WriteAllBytes(filename, bytes);
    }

    /// <summary>
    ///  Switch camera
    /// </summary>
    /// <param name="index"></param>
    void SwitchCamera(int index)
    {
        int i = 0;
        for (i = 0; i < cameras.Length; i++)
        {
            if (i != index)
            {
                if (_changeAudioListener)
                {
                    cameras[i].GetComponent<AudioListener>().enabled = false;
                }
                cameras[i].GetComponent<Camera>().enabled = false;
            }
            else
            {
                if (_changeAudioListener)
                {
                    cameras[i].GetComponent<AudioListener>().enabled = true;
                }
                cameras[i].GetComponent<Camera>().enabled = true;
            }
        }
    }

    /// <summary>
    /// Process switching camera
    /// </summary>
    void ProcSwitchCamera()
    {
        int i = 0;
        for (i = 0; i < cameras.Length; i++)
        {
            if (Input.GetKeyUp(shotcuts[i]))
            {
                SwitchCamera(i);
                print(System.String.Format("Camera switched to {0}", i + 1));
            }
        }
    }

    /// <summary>
    /// Capture images from a single camera
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="rect"></param>
    /// <param name="depth"></param>
    /// <returns></returns>
    Texture2D CaptureCamera(Camera camera, Rect rect, int depth)
    {
        RenderTexture rt = new RenderTexture((int)rect.width, (int)rect.height, depth);
        camera.targetTexture = rt;
        camera.Render();
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(rect, 0, 0);
        screenShot.Apply();

        camera.targetTexture = null;
        RenderTexture.active = null;
        //RenderTexture.active = null;
        GameObject.Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        string filename = Application.dataPath + "/Screenshot.png";
        File.WriteAllBytes(filename, bytes);

        return screenShot;
    }

    /// <summary>
    /// Capture images from two cameras
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="camera2"></param>
    /// <param name="rect"></param>
    /// <param name="depth"></param>
    /// <returns></returns>
    Texture2D CaptureCamera(Camera camera, Camera camera2, Rect rect, int depth)
    {
        RenderTexture rt = new RenderTexture((int)rect.width, (int)rect.height, depth);
        camera.targetTexture = rt;
        camera.Render();
        camera2.targetTexture = rt;
        camera2.Render();
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(rect, 0, 0);
        screenShot.Apply();

        camera.targetTexture = null;
        camera2.targetTexture = null;
        RenderTexture.active = null;
        GameObject.Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();
        string filename = Application.dataPath + "/Screenshot.png";
        File.WriteAllBytes(filename, bytes);

        return screenShot;
    }

    /// <summary>
    /// On GUI components
    /// </summary>
   // void OnGUI()
   // {
        //if (GUILayout.Button("Main-Capture"))
    //    {
            //this.CaptureCamera(mainCamera, new Rect(0, 0, Screen.width, Screen.height), 0);
            //print("Main Camera Capture");
      //  }

        //if (GUILayout.Button("Ventral-Capture"))
        //{
            //this.CaptureCamera(beeVentralEye, new Rect(0, 0, Screen.width, Screen.height), -1);
            //print("Ventral Camera Capture");
      //  }

        //if (GUILayout.Button("Right-Capture"))
        //{
        //    //this.CaptureCamera(beeRightEye, new Rect(0, 0, Screen.width, Screen.height), -1);
        //    print("Right Camera Capture");
        //}
    //}

    /// <summary>
    /// Draw line in the Unity
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="color"></param>
    /// <param name="duration"></param>
    void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
        lr.startColor = color;
        lr.endColor = color;
        //lr.SetColors(color, color);
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        //lr.SetWidth(0.1f, 0.1f);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        GameObject.Destroy(myLine, duration);
    }

    /// <summary>
    /// This sub-class implements a honeybee object with binocular vision.
    /// </summary>
    public class HoneyBee
    {
        #region FIELDS

        // frame width
        private int image_width;
        // frame height
        private int image_height;
        // total number of cells in an image
        private int Ncells;
        // left retina layer
        private float[,,] ventral_retina;
        // right retina layer
        //private float[,,] right_retina;
        // left lamina layer
        private float[,,] ventral_lamina;
        // right lamina layer
        //private float[,,] right_lamina;
        //private float[,,] left_medula;
        //private float[,,] right_medulla;
        //private float[] left_numwhite;
        //private float[] right_numwhite;
        // left output from medulla in time series
        private float[] ventral_output;
        // right output from medulla in time series
        //private float[] right_output;
        //private int index;
        // clip point in ON and OFF mechanisms
        private int clip;
        // delay frames
        private int delay_frames;
        // sampling distance or space between correlators
        private int sd;
        // bias in correlation
        private float bias;
        // time window in frames to compute means
        public int time_length;
        // local left threshold to compute the lambda
        private float threV;
        // local right threshold to compute the lambda
        //private float threR;
        // left numav value
        private float ventral_numav;
        // right numav value
        //private float right_numav;
        // left lambda value
        public float ventral_lambdapre;
        // right lambda value
        //public float right_lambdapre;
        // mean response from right side
        //public float meanR;
        // mean response from left side
        public float meanV;
        // detector response from left side
        public float ventral_response;
        // detector response from right side
        //public float right_response;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// default constructor
        /// </summary>
        public HoneyBee()
        {
            print("This constructs nothing......");
        }

        /// <summary>
        /// override constructor
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public HoneyBee(int width, int height)
        {
            image_width = width;
            image_height = height;
            Ncells = width * height;
            time_length = 16;//6
            ventral_retina = new float[height, width, time_length];
            //right_retina = new float[height, width, time_length];
            //right_lamina = new float[height, width, time_length];
            ventral_lamina = new float[height, width, time_length];
            //left_medula = new float[height, width, time_length];
            //right_medulla = new float[height, width, time_length];
            ventral_output = new float[time_length];
            //right_output = new float[time_length];
            //left_numwhite = new float[time_length];
            //right_numwhite = new float[time_length];
            //index = 1;
            clip = 0;
            delay_frames = 1; //4
            sd = 1;
            bias = 0.01f;
            //threR = 0.25f;
            //threL = 0.25f;
            ventral_numav = 0;
            //right_numav = 0;
            ventral_lambdapre = 0;
            //right_lambdapre = 0;
            //meanR = 0;
            meanV = 0;
            ventral_response = 0;
            //right_response = 0;

            print("A honey bee object contructed");
        }

        #endregion

        #region METHODS

        /// <summary>
        /// photoreceptor
        /// </summary>
        /// <param name="cur_input"></param>
        /// <param name="pre_input"></param>
        /// <returns></returns>
        private float Photoreceptor(float cur_input, float pre_input)
        {
            return cur_input - pre_input;
        }

        /// <summary>
        /// ON pass filter
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private float ONPass(float input)
        {
            if (input >= clip)
                return input;
            else
                return 0;
        }

        /// <summary>
        /// OFF pass filter
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private float OFFPass(float input)
        {
            if (input >= clip)
                return 0;
            else
                return Mathf.Abs(input);
        }

        /// <summary>
        /// matlab numf function
        /// </summary>
        /// <param name="inputMat"></param>
        /// <param name="t"></param>
        /// <param name="width"></param>
        /// <param name="local_thre"></param>
        /// <returns></returns>
        private float numf(float[,,] inputMat, int t, int width, float local_thre)
        {
            int localCount = 0;
            for (int j = 0; j < width; j++)
            {
                if (inputMat[1, j, t] > local_thre)
                    inputMat[1, j, t] = 1;
                else
                    inputMat[1, j, t] = 0;
            }
            for (int i = 0; i < width - 1; i++)
            {
                if (inputMat[1, i, t] - inputMat[1, i + 1, t] == 1)
                    localCount++;
            }

            return localCount;
        }

        /// <summary>
        /// calculate the lambda
        /// </summary>
        /// <param name="inputMat"></param>
        /// <param name="width"></param>
        /// <param name="numav"></param>
        /// <param name="thre"></param>
        /// <returns></returns>
        private float CalcLambdapre(float[,,] inputMat, int width, ref float numav, float thre)
        {
            float countSum = 0;
            for (int i = 0; i < time_length; i++)
            {
                float temp = numf(inputMat, i, width, thre);
                countSum += temp;
            }
            numav = countSum / time_length;
            if (numav <= 1)
            {
                return  2 * width;
            }
            else {
                return  2 * width / numav; ///
            }
           // return 2 * width / numav; ///
        }

        /// <summary>
        /// delay and correlation for each local cell
        /// </summary>
        /// <param name="input1"></param>
        /// <param name="delay_input1"></param>
        /// <param name="input2"></param>
        /// <param name="delay_input2"></param>
        /// <returns></returns>
        private float Correlator(float input1, float delay_input1, float input2, float delay_input2)
        {
            return delay_input1 * input2 - bias * input1 * delay_input2;
        }

        /// <summary>
        /// detector response to output
        /// </summary>
        /// <param name="lambdapre"></param>
        /// <param name="meanRes"></param>
        /// <returns></returns>
        private float DetectorResponse(float lambdapre, float meanRes)
        {
            float temp = lambdapre * Mathf.Sqrt(1 * meanRes);
            return 1*temp/(1+temp);
            //return  Mathf.Sqrt(1 * meanRes);
        }

        /// <summary>
        /// compute the local threshold in the numf function
        /// </summary>
        /// <param name="inputMat"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private float LocalThreshold(float[,,] inputMat, int t)
        {
            float minG = 1, maxG = 0;
            for (int i = 0; i < image_height; i++)
            {
                for (int j = 0; j < image_width; j++)
                {
                    if (minG > inputMat[i, j, t])
                        minG = inputMat[i, j, t];
                    if (maxG < inputMat[i, j, t])
                        maxG = inputMat[i, j, t];
                }
            }
            return (minG + maxG) / 2;
        }

        #endregion

        #region DETECTORS

        /// <summary>
        /// integrated processing
        /// </summary>
        /// <param name="ventralImgSeq"></param>        
        /// <param name="t"></param>
        public void AngularVelocityEstimator(float[,,] ventralImgSeq, int t)
        {
            // init time frames and etc.
            // current frame location
            int cur_time = t % time_length;
            // previous frame location
            int pre_time = (t - 1) % time_length;
            // delay frame location
            int delay_time = cur_time - delay_frames;
            if (delay_time < 0)
                delay_time = time_length - delay_frames;
            ventral_output[cur_time] = 0;
            //right_output[cur_time] = 0;
            float tmpsumV = 0;
            //float tmpsumR = 0;
            // define local threshold at the first frame only
            if (t == 1)
            {
                threV = LocalThreshold(ventralImgSeq, 0);
                //threR = LocalThreshold(rightImgSeq, 0);
                print(System.String.Format("threshold: {0:F}", threV)); // correct, checked
            }

            // retina and lamina layers processing
            for (int i = 0; i < image_height; i++)
            {
                for (int j = 0; j < image_width; j++)
                {
                    //retina
                    ventral_retina[i, j, cur_time] = Photoreceptor(ventralImgSeq[i, j, cur_time], ventralImgSeq[i, j, pre_time]);
                    //right_retina[i, j, cur_time] = Photoreceptor(rightImgSeq[i, j, cur_time], rightImgSeq[i, j, pre_time]);
                    //lamina on pass only
                    ventral_lamina[i, j, cur_time] = ONPass(ventral_retina[i, j, cur_time]);
                    //ventral_lamina[i, j, cur_time] = ONPass(ventral_retina[j, i, cur_time]);
                    //right_lamina[i, j, cur_time] = ONPass(right_retina[i, j, cur_time]);
                }
            }

            // compute the lambdapre
            ventral_lambdapre = CalcLambdapre(ventralImgSeq, image_width, ref ventral_numav, threV);
            //right_lambdapre = CalcLambdapre(rightImgSeq, image_width, ref right_numav, threR);

            // delay and correlation in medulla
            int numd = 1;
            for (int i = 0; i < image_height; i++)
            {
                for (int j = 0; j < image_width - sd; j++)
                {
                    //left_medula[i, j, cur_time] = Correlator(left_lamina[i, j, cur_time], left_lamina[i, j, delay_time], left_lamina[i, j + sd, cur_time], left_lamina[i, j + sd, delay_time]);
                    //right_medulla[i, j, cur_time] = Correlator(right_lamina[i, j, cur_time], right_lamina[i, j, delay_time], right_lamina[i, j + sd, cur_time], right_lamina[i, j + sd, delay_time]);
                    //ventral_output[cur_time] += Correlator(ventral_lamina[i, j + sd, cur_time], ventral_lamina[i, j + sd, delay_time], ventral_lamina[i, j, cur_time], ventral_lamina[i, j, delay_time]);

                    float temp = Correlator(ventral_lamina[i, j + sd, cur_time], ventral_lamina[i, j + sd, delay_time], ventral_lamina[i, j, cur_time], ventral_lamina[i, j, delay_time])/255;
                    //float temp = Correlator(ventral_lamina[j, i + sd, cur_time], ventral_lamina[j, i + sd, delay_time], ventral_lamina[j, i, cur_time], ventral_lamina[j, i, delay_time]) / 255;
                    if (temp > 0.001f)
                    {
                        ventral_output[cur_time] += temp;
                        numd += 1;
                    }

                    //right_output[cur_time] += Correlator(right_lamina[i, j, cur_time], right_lamina[i, j, delay_time], right_lamina[i, j + sd, cur_time], right_lamina[i, j + sd, delay_time]);
                }
            }
            ventral_output[cur_time] /= numd;

            // compute the average response in the define time window
            //ventral_output[cur_time] /= Ncells;
            //right_output[cur_time] /= Ncells;
            int numd2 = 1;
            for (int ii = 0; ii < time_length; ii++)
            {
                if (ventral_output[ii] > 0.001f) {
                    tmpsumV += ventral_output[ii];
                    numd2 += 1;
                }
               
            }
            meanV = tmpsumV / numd2;
            //meanR = tmpsumR / time_length;

            // compute the left and right estimators outputs
            ventral_response = DetectorResponse(ventral_lambdapre, meanV);
            //right_response = DetectorResponse(right_lambdapre, meanR);
            //print(System.String.Format("ventral_response: {0:F}", ventral_response));

        }

        #endregion
    }
}

