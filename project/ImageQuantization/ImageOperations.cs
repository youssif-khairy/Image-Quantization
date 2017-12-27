using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using Priority_Queue;
///Algorithms Project
///Intelligent Scissors
///

namespace ImageQuantization
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel //given struct that contain red and green and blue for each pixel
    {
        public byte red, green, blue;
        public RGBPixel(byte r, byte g, byte b)// paramatarized constructor that intitialized red and green and blue
        {
            red = r;
            green = g;
            blue = b;
        }
    }
    public struct RGBPixelD
    {
        public double red, green, blue;

    }
    public class GRAPH : FastPriorityQueueNode //calss that construct edges
    {
        public int from;//from which node this edge come
        public int to;//to which node this edge come
        public float edge;// the value of edge between from and to nodes
        public GRAPH(int f, int t, float e)//paramatrized constructor to intitialze the values of class
        {
            from = f;
            to = t;
            edge = e;
        }
        public GRAPH()//default constructor just for intitializng values only
        {
        }
        
    }
    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {
        static List<int> nodes;//collect distincit nodes
        static List<GRAPH> MST;//minimum spanning tree list
        public static Dictionary<int, List<int>> adj_lst;//adjacency list for all nodes
        public static int[] Node_Cluster;// for each node what cluster does it in
        public static FastPriorityQueue<GRAPH> H;//periority_queue for collecting large edges from MST 
        public static Dictionary<GRAPH, float> VX;//only help the periority_queue to check existance 
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[0];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[2];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[0] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[2] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }


        /// <summary>
        /// Apply Gaussian smoothing filter to enhance the edge detection 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="filterSize">Gaussian mask size</param>
        /// <param name="sigma">Gaussian sigma</param>
        /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }

            return Filtered;
        }

        public static void get_Distincit(RGBPixel[,] M)//function to get the distincit nodes and take given matrix of image
        {
            /*
             Total complexty = theta (w*h) = theta (N^2)
             */
            nodes = new List<int>();//intitialize the distincit list
            HashSet<int> Vertices = new HashSet<int>();//hashset to check if node is already exist
            MST = new List<GRAPH>();//initialize Minimum spaning tree list
            int h = M.GetLength(0), w = M.GetLength(1);// get width and height of image
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    int ind1 = M[i, j].red << 8 | M[i, j].green;
                    int ind = ind1 << 8 | M[i, j].blue;
                    /*shifting the red and green and blue in integer and work as integer this is much faster than 
                     making it RGBPixel due to much of collision of hashset as it use hash function */
                    if (Vertices.Add(ind))//if this node is not already taken before then add it to hashset and
                    {
                        nodes.Add(ind);// add it as it is distincit color
                        MST.Add(new GRAPH(ind, ind, (float)10000000));
                        //initialize itself in Minimum spanning tree as it go from itself to itself with large value
                    }//if
                }//inner for
            }//outer for
          
        }//get_distincit


        public static void grph(int CLUS)// graph construction and getting edges that will be cut that take number of clusters
        {
            /*
             Total complexty = O (E log (k))   
                         --where E is the number of edges and K is the number of clusters wanted to cut
             */
            float min = 10000000, newedge;
            //min is minimum value that take from moving from exesting node to another node, newedge is edge between exesting node to another node
            int k = 0, count = nodes.Count;//k is the index of minimum node that i would start in next time
            bool[] vstd = new bool[16777216];//to check if node has been visited before (get values between it and all another none visited nodes)
            VX = new Dictionary<GRAPH, float>();// initialize th helper of periority_queue
            H = new FastPriorityQueue<GRAPH>(CLUS - 1);//initialize periority_queue

            for (int i = k; ; )//first loop to move in distincit node
            {
                bool ch = false;// if it still false then i can't go anywhere because all nodes have been visited 
                if (vstd[nodes[i]]) continue;// if the node that i stand in is visited then continue
                vstd[nodes[i]] = true;//this not is not visited before then make it as visited
                min = 10000000;// give minimum every time very large value
                for (int j = 0; j < count; j++)//second loop to move in distincit node
                {
                    if (vstd[nodes[j]]) continue;//if the node that i stand in is visited then continue
                    ch = true;//then it i found another node to move in
                    byte[] b = BitConverter.GetBytes(nodes[i]), bb = BitConverter.GetBytes(nodes[j]);
                    // convert the shifted integer to red and green and blue
                    newedge = (((b[0] - bb[0]) * (b[0] - bb[0])) + ((b[1] - bb[1]) * (b[1] - bb[1])) + ((b[2] - bb[2]) * (b[2] - bb[2])));
                    //calculate the edge between to nodes
                    if (newedge < min)//if the edge i have less than minimum then 
                    {
                        min = newedge;//make the minimum is newedge
                        k = j;//and save its index to start from it the next time
                    }
                    if (MST[j].edge < min) // if the edge that is already saved is less than minimum then 
                    {
                        min = MST[j].edge; //make the minimum is saved edge
                        k = j; //and save its index to start from it the next time
                    }
                    if (newedge < (float)MST[j].edge) // if the calculated edge before is larger than the newly calculated edge then
                    {
                        MST[j].edge = newedge;//then replace the calculaed edge with the new minimum edge
                        MST[j].from = nodes[i];//and change from where it comes
                    }
                }
                i = k;//K is the minimum index that i will start the loop with
                if (ch == false) // then i cant go to any node again because all nodes have been visited then choose the cutted edges
                {
                    GRAPH T = new GRAPH();//temp to delete the deleted edge from dictionary as it is deleted from periority_queue 
                    for (int y = 1; y < MST.Count; y++)//move in all Minimum spanning tree 
                    {
                        if (H.Count < CLUS - 1)// if the periority_queue is not filled with required number of clusters then  
                        {
                            H.Enqueue(MST[y], MST[y].edge);// add in periority_queue with complexty O(log K) k is the wanted number of clusters
                            VX.Add(MST[y], MST[y].edge);// add in the dictionary this edge
                        }
                        else//if the periority_queue is  filled with required number of clusters then  
                        {
                            if (H.First.edge < MST[y].edge)//check if the edge that i stand is larger than the top of queue that contain the smallest edge
                            {
                                T = H.Dequeue();//pop the smallest from the queue 
                                VX.Remove(T);//remove also this node from dictionary
                                H.Enqueue(MST[y], MST[y].edge);//enqueue the new large node with the new large edge with complexty O(log K)
                                VX.Add(MST[y], MST[y].edge);//add the new large node with the new large edge
                            }
                        }
                    }

                }
                if (!ch) break; //if i can't go anywhere then break
            }//main loop
            /* this to get cost of minimum spanning tree*/
          /*  double sum = 0;
            for (int i = 1; i < MST.Count; i++)
            {
                sum += Math.Sqrt(MST[i].edge);
            }
            MessageBox.Show(sum.ToString());*/

        }
        /*
         this used in dfs
         */
        static bool[] vis;//check if node is visited
        static int Number_Nodes = 0, Cluster_Number = 0;//number of nodes in the cluster,cluster number 
        static double Rd = 0, G = 0, B = 0;//red and green and blue accumilated in the cluster

        public static void dfs(int node)// move in adjacency list to take the cluster and take the node that i will move in it
        {
            /*
             total complexty of O(D+E) = O(D) -- D is distincit colors and E is the the number of edges
             */
            vis[node] = true;// make this node as visited not to take in any other class
            byte[] b = BitConverter.GetBytes(node);// resplit the node to red and green and blue to accumilate it
            Rd += b[2]; G += b[1]; B += b[0];// accumilate the red and the green and the blue
            Number_Nodes++;//increment the number of nodes in cluster
            Node_Cluster[node] = Cluster_Number;//this node is in cluster ..
            int count = adj_lst[node].Count;
            for (int i = 0; i < count; i++)// move in childs of the node
            {
                if (!vis[adj_lst[node][i]])//if it is not visited then
                {
                    dfs(adj_lst[node][i]);//dfs it
                }
            }//for

        }//dfs

        public static void Clustring()// make clustring
        {
            /*
             Total complexty  is O(MST count) = distincit + O(d log K) 
             */
            adj_lst = new Dictionary<int, List<int>>();// initialize adjacency list
            Node_Cluster = new int[16777216]; // which node assigned to which cluster
            vis = new bool[16777216];// visited array described before
            RGBPixel Val;


            int sz1 = MST.Count;
            for (int i = 1; i < sz1; i++)// mov in size of mst = d
            {

                if (VX.ContainsKey(MST[i]))// if this edge will be cutt
                {
                    /*add it to adjecency list*/
                    if (!adj_lst.ContainsKey(MST[i].from))
                    {
                        adj_lst.Add(MST[i].from, new List<int>());
                    }
                    if (!adj_lst.ContainsKey(MST[i].to))
                    {
                        adj_lst.Add(MST[i].to, new List<int>());
                    }
                }
                else // if this edge will not be cutt
                {
                    /*add it to adjecency list*/
                    if (!adj_lst.ContainsKey(MST[i].from))
                    {
                        adj_lst.Add(MST[i].from, new List<int> { MST[i].to });
                    }
                    else
                    {
                        adj_lst[MST[i].from].Add(MST[i].to);
                    }
                    if (!adj_lst.ContainsKey(MST[i].to))
                    {
                        adj_lst.Add(MST[i].to, new List<int> { MST[i].from });
                    }
                    else
                    {
                        adj_lst[MST[i].to].Add(MST[i].from);
                    }
                }

            }

            int sz = H.Count;
            while (sz > 0)// move in the number of cutted edges 
            {
                GRAPH R = H.Dequeue(); // dequeue the first edge with complexty O(log K)
                sz--;
                Number_Nodes = 0;

                if (!vis[R.from]) // if the from is not visited
                {
                    Number_Nodes = 0; Rd = G = B = 0; dfs(R.from); Rd /= Number_Nodes; G /= Number_Nodes; B /= Number_Nodes;
                    Val.red = (byte)Rd; Val.green = (byte)G; Val.blue = (byte)B;
                    /*shift the accumilated red green blue from this cluster*/
                    int ind1 = Val.red << 8 | Val.green;
                    int ind = ind1 << 8 | Val.blue;
                    nodes[Cluster_Number] = ind;// reuse the nodes to add the average of this cluster
                    Cluster_Number++;
                }
                if (!vis[R.to])// if the from is not visited
                {
                    Number_Nodes = 0; Rd = G = B = 0; dfs(R.to); Rd /= Number_Nodes; G /= Number_Nodes; B /= Number_Nodes;
                    Val.red = (byte)Rd; Val.green = (byte)G; Val.blue = (byte)B;
                    /*shift the accumilated red green blue from this cluster*/
                    int ind1 = Val.red << 8 | Val.green;
                    int ind = ind1 << 8 | Val.blue;
                    nodes[Cluster_Number] = ind;// reuse the nodes to add the average of this cluster
                    Cluster_Number++;
                }

            }


        }//Clustring


        public static RGBPixel[,] Refill_Mtrx(RGBPixel[,] M)//refill the matrix again to get it back to image
        {

            int h = M.GetLength(0), w = M.GetLength(1), d1, d2;//get width and height of matrix
            for (int i = 0; i < h; i++)//height
            {
                for (int j = 0; j < w; j++)//width
                {
                    /*shift this cell to know its cluster*/
                    d1 = M[i, j].red << 8 | M[i, j].green;
                    d2 = d1 << 8 | M[i, j].blue;
                    byte[] b = BitConverter.GetBytes(nodes[Node_Cluster[d2]]);//split this cluster average into red and green and blue
                   //regive this value with its cluster average
                    M[i, j].red = b[2];
                    M[i, j].green = b[1];
                    M[i, j].blue = b[0];
                }
            }

            return M;// return this matrix
        }




    }
}
