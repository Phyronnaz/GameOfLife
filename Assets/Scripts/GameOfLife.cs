﻿using UnityEngine;
using System.Threading;
using System.Diagnostics;

namespace Assets.Scripts
{
    public class GameOfLife
    {
        //Stay alive
        public static int W = 1;
        public static int X = 4;
        //Became alive
        public static int Y = 2;
        public static int Z = 3;
        //Size of a thread
        public static int ThreadSize = 30;
        //Size of a chunk (<40)
        public static int ChunkSize = 39;

        public bool Busy;

        readonly Material RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial;
        bool[][,,] worlds = new bool[3][,,];
        GameOfLifeRenderer[] gameOfLifeRenderers;
        int currentWorldIndex;
        ManualResetEvent[] nextWaitHandles;
        ManualResetEvent[] cubesWaitHandles;
        bool cubesUpdateWaiting;
        bool cubesUpdateInProgress;
        bool nextInProgress;
        Stopwatch trianglesStopwatch;
        Stopwatch meshesStopwatch;
        Stopwatch computationStopwatch;

        public int XSize { get { return worlds[0].GetLength(0); } }
        public int YSize { get { return worlds[0].GetLength(1); } }
        public int ZSize { get { return worlds[0].GetLength(2); } }


        public GameOfLife(int XSize, int YSize, int ZSize,
            Material redMaterial, Material whiteMaterial, Material greenMaterial, Material yellowMaterial)
        {
            worlds[0] = new bool[XSize, YSize, ZSize];
            worlds[1] = new bool[XSize, YSize, ZSize];
            worlds[2] = new bool[XSize, YSize, ZSize];

            RedMaterial = redMaterial;
            WhiteMaterial = whiteMaterial;
            GreenMaterial = greenMaterial;
            YellowMaterial = yellowMaterial;

            InitRenderers();
        }



        public void SetWorld(bool[,,] world)
        {
            var size = world.GetLength(0);
            worlds[0] = new bool[size, size, size];
            worlds[1] = world;
            worlds[2] = new bool[size, size, size];

            currentWorldIndex = 1;

            InitRenderers();
        }

        public bool[,,] GetWorld(int index)
        {
            return worlds[(worlds.Length + currentWorldIndex + index) % worlds.Length];
        }


        public void SetBlock(int x, int y, int z, bool value)
        {
            GetWorld(-1)[x, y, z] = value;
        }

        public void ApplyBlocksChanges()
        {
            var waitHandles = CalculateNextWorld(GetWorld(-1), GetWorld(0));
            foreach (var w in waitHandles)
            {
                w.WaitOne();
            }
        }


        public void Update()
        {
            if (nextInProgress)
            {
                bool ended = true;
                foreach (var w in nextWaitHandles)
                {
                    ended = ended && w.WaitOne(0);
                }
                if (ended)
                {
                    nextInProgress = false;
                    currentWorldIndex++;
                    UnityEngine.Debug.Log("Computation time: " + computationStopwatch.ElapsedMilliseconds.ToString() + "ms");
                    computationStopwatch.Stop();
                }
                Busy = nextInProgress;
            }
            if (cubesUpdateWaiting && !nextInProgress)
            {
                cubesUpdateWaiting = false;
                cubesUpdateInProgress = true;
                cubesWaitHandles = UpdateTriangles();
                trianglesStopwatch = new Stopwatch();
                trianglesStopwatch.Start();
            }
            if (cubesUpdateInProgress)
            {
                bool ended = true;
                foreach (var w in cubesWaitHandles)
                {
                    ended = ended && w.WaitOne(0);
                }
                if (ended)
                {
                    cubesUpdateInProgress = false;
                    UnityEngine.Debug.Log("Triangles Calcul time: " + trianglesStopwatch.ElapsedMilliseconds.ToString() + "ms");
                    trianglesStopwatch.Stop();
                    meshesStopwatch = new Stopwatch();
                    meshesStopwatch.Start();
                    UpdateMeshes();
                    UnityEngine.Debug.Log("Meshes Update time: " + meshesStopwatch.ElapsedMilliseconds.ToString() + "ms");
                    meshesStopwatch.Stop();
                }
                Busy = cubesUpdateInProgress;
            }
        }

        public void Next()
        {
            if (Busy)
            {
                UnityEngine.Debug.LogWarning("Not able to calculate next: Busy");
            }
            else
            {
                nextWaitHandles = CalculateNextWorld(GetWorld(0), GetWorld(1));
                nextInProgress = true;
                computationStopwatch = new Stopwatch();
                computationStopwatch.Start();
            }
        }

        public void UpdateCubes()
        {
            if (Busy)
            {
                UnityEngine.Debug.LogWarning("Not able to update cubes: Busy");
            }
            else
            {
                cubesUpdateWaiting = true;
            }
        }




        private void InitRenderers()
        {
            if (gameOfLifeRenderers != null)
            {
                foreach (var g in gameOfLifeRenderers)
                {
                    GameObject.Destroy(g.gameObject);
                }
            }
            gameOfLifeRenderers = new GameOfLifeRenderer[(XSize / ChunkSize + 1) * (YSize / ChunkSize + 1) * (ZSize / ChunkSize + 1)];
            for (int x = 0; x < XSize / ChunkSize + 1; x++)
            {
                for (int y = 0; y < YSize / ChunkSize + 1; y++)
                {
                    for (int z = 0; z < ZSize / ChunkSize + 1; z++)
                    {
                        gameOfLifeRenderers[x + (XSize / ChunkSize + 1) * (y + (YSize / ChunkSize + 1) * z)] = new GameOfLifeRenderer(
                            ChunkSize * x, Mathf.Min(XSize, ChunkSize * (x + 1)),
                            ChunkSize * y, Mathf.Min(XSize, ChunkSize * (y + 1)),
                            ChunkSize * z, Mathf.Min(XSize, ChunkSize * (z + 1)),
                            RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial);
                    }
                }
            }
        }

        private ManualResetEvent[] UpdateTriangles()
        {
            var waitHandles = new ManualResetEvent[gameOfLifeRenderers.Length];

            for (int i = 0; i < gameOfLifeRenderers.Length; i++)
            {
                var x = i;
                waitHandles[x] = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(state => gameOfLifeRenderers[x].UpdateTriangles(waitHandles[x], GetWorld(0), GetWorld(1), GetWorld(2)));
            }
            return waitHandles;
        }

        private void UpdateMeshes()
        {
            for (int i = 0; i < gameOfLifeRenderers.Length; i++)
            {
                gameOfLifeRenderers[i].UpdateMeshes();
            }
        }

        private ManualResetEvent[] CalculateNextWorld(bool[,,] currentWorld, bool[,,] nextWorld)
        {
            var waitHandles = new ManualResetEvent[(XSize / ThreadSize + 1) * (YSize / ThreadSize + 1) * (ZSize / ThreadSize + 1)];

            for (int i = 0; i < XSize / ThreadSize + 1; i++)
            {
                for (int j = 0; j < YSize / ThreadSize + 1; j++)
                {
                    for (int k = 0; k < ZSize / ThreadSize + 1; k++)
                    {
                        var x = i;
                        var y = j;
                        var z = k;
                        waitHandles[x + (XSize / ThreadSize + 1) * (y + (YSize / ThreadSize + 1) * z)] = new ManualResetEvent(false);
                        ThreadPool.QueueUserWorkItem(state => Thread(currentWorld, nextWorld,
                                                                    ThreadSize * x, Mathf.Min(XSize, ThreadSize * (x + 1)),
                                                                    ThreadSize * y, Mathf.Min(YSize, ThreadSize * (y + 1)),
                                                                    ThreadSize * z, Mathf.Min(ZSize, ThreadSize * (z + 1)),
                                                                    waitHandles[x + (XSize / ThreadSize + 1) * (y + (YSize / ThreadSize + 1) * z)]));
                    }
                }
            }
            return waitHandles;
        }


        private static void Thread(bool[,,] currentWorld, bool[,,] nextWorld,
            int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd,
            ManualResetEvent waitHandle)
        {
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    for (int z = zStart; z < zEnd; z++)
                    {
                        int neighbors = currentWorld[x, y, z] ? -1 : 0;
                        for (int i = -1; i < 2; i++)
                        {
                            for (int j = -1; j < 2; j++)
                            {
                                for (int k = -1; k < 2; k++)
                                {
                                    if (0 <= x + i && x + i < xEnd &&
                                        0 <= y + j && y + j < yEnd &&
                                        0 <= z + k && z + k < zEnd)
                                    {
                                        if (currentWorld[x + i, y + j, z + k])
                                        {
                                            neighbors++;
                                        }
                                    }
                                }
                            }
                        }
                        nextWorld[x, y, z] = (Y < neighbors && neighbors < Z) || (W < neighbors && neighbors < X && currentWorld[x, y, z]);
                    }
                }
            }
            waitHandle.Set();
        }
    }
}
