﻿using System;
using System.Collections;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GamePuppet
{
    public class PuppetRequestHandler
    {
        internal static PuppetDriverResponse HandlePuppetDriverRequest(PuppetDriverRequest request)
        {
            PuppetDriverResponse response = new PuppetDriverResponse {method = request.method };
            Vector2 swipeDirection = Vector2.zero;

            switch (request.method.ToLowerInvariant())
            {
                case "registereditor":
                    response.result = FindGameObject(request.name, request.parent, gameObject => true.ToString(), false.ToString());
                    break;
                case "exist":
                    response.result = FindGameObject(request.name, request.parent, gameObject => true.ToString(), false.ToString());
                    break;
                case "active":
                    response.result = FindGameObject(request.name, request.parent, gameObject => gameObject.activeInHierarchy.ToString(), false.ToString());
                    break;
                /*case "getcomponent":
                    responseString = FindGameObject(request.name, request.parent, gameObject =>
                    {
                        var component = gameObject.GetComponent(data["component"]);
                        return component != null ? EditorJsonUtility.ToJson(component) : "null";
                    });
                    break;*/

                case "click":
                    response.result = FindGameObject(request.name, request.parent, go =>
                    {
                        var pointer = new PointerEventData(EventSystem.current);
                        ExecuteEvents.Execute(go, pointer, ExecuteEvents.pointerClickHandler);
                        return "success";
                    });
                    break;

                case "swipeleft":
                    swipeDirection = Vector2.left;
                    goto case "swipe";
                case "swiperight":
                    swipeDirection = Vector2.right;
                    goto case "swipe";
                case "swipeup":
                    swipeDirection = Vector2.up;
                    goto case "swipe";
                case "swipedown":
                    swipeDirection = Vector2.down;
                    goto case "swipe";

                case "swipe":
                    swipeDirection *= 100;

                    response.result = FindGameObject(request.name, request.parent, go =>
                    {
                        var pointer = new PointerEventData(EventSystem.current);

                        var rt = (RectTransform)go.transform;

                        Vector3[] corners = new Vector3[4];
                        rt.GetWorldCorners(corners);
                        var bottomLeft = Camera.main.WorldToScreenPoint(corners[0]);
                        var topLeft = Camera.main.WorldToScreenPoint(corners[1]);
                        var topRight = Camera.main.WorldToScreenPoint(corners[2]);
                        var bottomRight = Camera.main.WorldToScreenPoint(corners[3]);

                        var center = new Vector2(topLeft.x + (bottomRight.x - topLeft.x) / 2, bottomRight.y + (topLeft.y - bottomRight.y) / 2);

                        go.GetComponent<MonoBehaviour>().StartCoroutine(DragCoroutine(go, pointer, center, swipeDirection));

                        return "success";
                    });
                    break;


                case "sendkeys":
                    response.result = FindGameObject(request.name, request.parent, go =>
                    {
                        var input = go.GetComponent<InputField>();
                        if (input != null)
                        {
                            input.text = request.value;
                        }
                        else
                        {
                            return "input not found";
                        }

                        return "success";
                    });
                    break;

                case "startplaymode":
                    EditorApplication.update += StartPlayMode;
                    response.result = "success";
                    break;
                case "stopplaymode":
                    response.result = InvokeOnMainThreadAndWait(() =>
                    {
                        EditorApplication.isPlaying = false;
                    });
                    break;

                case "ping":
                    response.result = "pong";
                    break;
                case "takescreenshot":
                    var path = request.value;
                    PuppetProcessor.QueueOnMainThread(() => { TakeScreenshot(path); });
                    response.result = "success";
                    break;

                default:
                    response.result = "Unknown method " + request.method + ".";
                    break;
            }

            return response;
        }

        private static IEnumerator DragCoroutine(GameObject go, PointerEventData dragPointer, Vector2 goCenter, Vector2 dragDelta)
        {
            dragPointer.position = goCenter;
            dragPointer.delta = dragDelta;

            ExecuteEvents.Execute(go, dragPointer, ExecuteEvents.beginDragHandler);

            for (var i = 0; i < 2; i++)
            {
                ExecuteEvents.Execute(go, dragPointer, ExecuteEvents.dragHandler);

                dragPointer.position += dragDelta;
                yield return null;
            }

            ExecuteEvents.Execute(go, dragPointer, ExecuteEvents.endDragHandler);
        }

        private static void StartPlayMode()
        {
            EditorApplication.update -= StartPlayMode;

            EditorApplication.isPlaying = true;
        }

        private static string FindGameObject(string nameOrPath, string parent, Func<GameObject, string> onComplete, string notFoundMsg = null)
        {
            // event used to wait the answer from the main thread.
            AutoResetEvent autoEvent = new AutoResetEvent(false);

            string response = "";
            PuppetProcessor.QueueOnMainThread(() =>
            {
                try
                {
                    var go = PuppetProcessor.FindGameObject(nameOrPath, parent);
                    if (go != null)
                    {
                        response = onComplete(go);
                    }
                    else
                    {
                        if (notFoundMsg != null)
                        {
                            response = notFoundMsg;
                        }
                        else
                        {
                            response = "not found (" + (parent != null ? parent + "/" : "") + nameOrPath + ")";
                        }
                    }
                }
                catch (Exception e)
                {
                    Log(e);
                    response = e.Message;
                }
                finally
                {
                    // set the event to "unlock" the thread
                    autoEvent.Set();
                }
            });

            // wait for the end of the 'action' executed in the main thread
            autoEvent.WaitOne();

            return response;
        }

        private static string InvokeOnMainThreadAndWait(Action action)
        {
            // event used to wait the answer from the main thread.
            AutoResetEvent autoEvent = new AutoResetEvent(false);

            string response = "OK";
            PuppetProcessor.QueueOnMainThread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Log(e);
                    response = e.Message;
                }
                finally
                {
                    // set the event to "unlock" the thread
                    autoEvent.Set();
                }
            });

            // wait for the end of the 'action' executed in the main thread
            autoEvent.WaitOne();

            return response;
        }

        public static void Log(string msg)
        {
            Debug.Log(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " [Puppet] " + msg);
        }

        private static void Log(Exception e)
        {
            Debug.Log(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " [Puppet] " + e);
        }

        private static void TakeScreenshot(string pathName)
        {
            ScreenCapture.CaptureScreenshot(pathName);
        }
    }
}