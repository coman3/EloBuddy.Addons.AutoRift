using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoRift.Properties;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Color = System.Drawing.Color;

namespace AutoRift.Utilities.Pathfinder
{
    internal class NavGraph
    {
        private Vector3 _gate1, _gate2;
        public Node[] Nodes;
        public readonly ColorBGRA NodeColor;
        public readonly Color LineColor;
        private readonly string _navFile;
        private NavGraphTest _test;

        public NavGraph(string dir)
        {
            if (ObjectManager.Player.Team == GameObjectTeam.Chaos)
            {
                _gate1=new Vector3(2685, 4784, 75);
                _gate2 = new Vector3(4813, 2806, 79);
            }
            else
            {
                _gate1 = new Vector3(10018, 12144, 75);
                _gate2 = new Vector3(12078, 10104, 79);
            }
            _navFile = Path.Combine(dir + "\\" + "NavGraph" + Game.MapId + ".txt");
            NodeColor = new ColorBGRA(50, 200, 0, 255);
            LineColor = Color.Gold;
            Load();
            Chat.OnInput += Chat_OnInput;
        }

        void Chat_OnInput(ChatInputEventArgs args)
        {
            if(_test!=null||!args.Input.Equals("/navgraph")) return;
            args.Process = false;
            _test=new NavGraphTest(this);
        
        }

        public void Save()
        {
            using (FileStream f = new FileStream(_navFile, FileMode.Create, FileAccess.Write))
            {
                f.Write(BitConverter.GetBytes(Nodes.Length), 0, 4);
                foreach (Node node in Nodes)
                {
                    node.Serialize(f);
                }
            }
        }



        private void load(string file)
        {
            byte[] buffer = new byte[4];
            using (FileStream f = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                f.Read(buffer, 0, 4);
                Nodes=new Node[BitConverter.ToInt32(buffer, 0)];
                for (int i = 0; i < Nodes.Length; i++)
                {
                    Nodes[i]=new Node(f, buffer, this);
                    Nodes[i].Passable = Nodes[i].Position.Distance(_gate1) > 200 &&
                                        Nodes[i].Position.Distance(_gate2) > 200;
                }
            }
        }

        private void load()
        {
            byte[] buffer = new byte[4];
            using (MemoryStream f = new MemoryStream(Resources.NavGraphSummonersRift))
            {
                f.Read(buffer, 0, 4);
                Nodes = new Node[BitConverter.ToInt32(buffer, 0)];
                for (int i = 0; i < Nodes.Length; i++)
                {
                    Nodes[i] = new Node(f, buffer, this);
                    Nodes[i].Passable = Nodes[i].Position.Distance(_gate1) > 200 &&
                    Nodes[i].Position.Distance(_gate2) > 200;
                }
            }
        }

        public void Load()
        {
            if (!File.Exists(_navFile))
                load();
            else
            {
                load(_navFile);
            }
        }

        public void AddNode(Vector3 pos)
        {
            Node[] tmp = new Node[Nodes.Length + 1];
            Nodes.CopyTo(tmp, 0);
            Nodes = tmp;
            Nodes[Nodes.Length - 1] = new Node(new int[0], pos, this);

        }
        public void RemoveNode(int node)
        {

            while (Nodes[node].Neighbors.Length > 0)
            {
                RemoveLink(node, Nodes[node].Neighbors[0]);
            }
            Node[] tmp = new Node[Nodes.Length - 1];
            Array.Copy(Nodes, 0, tmp, 0, node);
            Array.Copy(Nodes, node + 1, tmp, node, Nodes.Length - node - 1);
            Nodes = tmp;
            for (int i = 0; i < Nodes.Length; i++)
            {
                for (int j = 0; j < Nodes[i].Neighbors.Length; j++)
                {
                    if (Nodes[i].Neighbors[j] > node)
                        Nodes[i].Neighbors[j]--;
                }
            }
        }

        public int FindClosestNode(Vector3 pos)
        {
            float minDist = float.MaxValue;
            int node = -1;
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (!(pos.Distance(Nodes[i].Position) < minDist)) continue;
                minDist = pos.Distance(Nodes[i].Position);
                node = i;
            }
            return node;
        }
        public int FindClosestNode(Vector3 pos, Vector3 end)
        {
            float minDist = float.MaxValue;
            int node = -1;
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (!(pos.Distance(Nodes[i].Position) < minDist) || end.Distance(Nodes[i].Position) > end.Distance(ObjectManager.Player)) continue;
                minDist = pos.Distance(Nodes[i].Position);
                node = i;
            }
            return node;
        }

        public int FindClosestNode(Vector3 pos, int except)
        {
            float minDist = float.MaxValue;
            int node = -1;
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (except == i || !(pos.Distance(Nodes[i].Position) < minDist || !Nodes[i].Passable)) continue;
                minDist = pos.Distance(Nodes[i].Position);
                node = i;
            }
            return node;
        }

        public int FindClosestNode(int nodeId)
        {

            float minDist = float.MaxValue;
            int node = 0;
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (nodeId == i || Nodes[nodeId].Position.Distance(Nodes[i].Position) > minDist||!Nodes[i].Passable) continue;
                minDist = Nodes[nodeId].Position.Distance(Nodes[i].Position);
                node = i;
            }
            return node;
        }

        public void AddLink(int node1, int node2)
        {
            Nodes[node1].AddNeighbor(node2);
            Nodes[node2].AddNeighbor(node1);
        }
        public void RemoveLink(int node1, int node2)
        {
            Nodes[node1].RemoveNeighbor(node2);
            Nodes[node2].RemoveNeighbor(node1);
        }
        public bool LinkExists(int node1, int node2)
        {
            return Nodes[node1].Neighbors.Contains(node2);
        }

        public void Draw()
        {
            for (int i = 0; i < Nodes.Length; i++)
            {
                Nodes[i].DrawPositions();
                //if(Nodes[i].position.IsOnScreen())
                   // Drawing.DrawText(Nodes[i].position.WorldToScreen(), Color.Gold, "      " + i + "    " + Nodes[i].Neighbors.Length, 10);
            }
            for (int i = 0; i < Nodes.Length; i++)
            {
                Nodes[i].DrawLinks();
            }

        }

        public List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            PathNode p = FindPath(FindClosestNode(start), FindClosestNode(end));
            List<Vector3> ret = new List<Vector3> {end};
            while (p!=null)
            {
                ret.Add(Nodes[p.Node].Position);
                p = p.Parent;
            }
            ret.Reverse();
            return ret;
        }
        public List<Vector3> FindPath2(Vector3 start, Vector3 end)
        {
            PathNode p = FindPath(FindClosestNode(start, end), FindClosestNode(end));
            List<Vector3> ret = new List<Vector3> { end };
            while (p != null)
            {
                ret.Add(Nodes[p.Node].Position);
                p = p.Parent;
            }
            ret.Reverse();
            return ret;
        }
        public List<Vector3> FindPathRandom(Vector3 start, Vector3 end)
        {
            PathNode p = FindPath(FindClosestNode(start, end), FindClosestNode(end));
            List<Vector3> ret = new List<Vector3> { end };
            while (p != null)
            {
                ret.Add(Nodes[p.Node].Position.Randomized(-15f, 15f));
                p = p.Parent;
            }
            ret.Reverse();
            return ret;
        }

        private PathNode FindPath(int startNode, int endNode)
        {
            if (startNode == -1 || endNode == -1) return null;
            List<PathNode> open=new List<PathNode>();
            List<PathNode> closed = new List<PathNode>();
            open.Add(new PathNode(0, Nodes[startNode].GetDistance(endNode), startNode, null));


            while (open.Count > 0)
            {
                PathNode q = open.OrderBy(n => n.FCost).First();
                open.Remove(q);
                foreach (int neighbor in Nodes[q.Node].Neighbors)
                {
                    if (!Nodes[neighbor].Passable)
                        continue;
                    PathNode s=new PathNode(q.GCost+
                        Distance(q, neighbor) * (Nodes[neighbor].Position.GetNearestTurret().Distance(Nodes[neighbor].Position)<900?20:1)
                        
                        
                        ,Distance(endNode, neighbor), neighbor, q );
                    if (neighbor == endNode)
                    {
                        return s;
                    }
                    PathNode sameNodeOpen = open.FirstOrDefault(el => el.Node == neighbor);
                    if (sameNodeOpen != null)
                    {
                        if(sameNodeOpen.FCost<s.FCost) continue;
                    }
                    PathNode sameNodeClosed = closed.FirstOrDefault(el => el.Node == neighbor);
                    if (sameNodeClosed != null)
                    {
                        if(sameNodeClosed.FCost<s.FCost) continue;
                    }
                    open.Add(s);
                }
                closed.Add(q);
            }
            return null;

        }


        private float Distance(PathNode p1, int p2)
        {
            return Nodes[p1.Node].Position.Distance(Nodes[p2].Position);
        }

        private float Distance(int p1, int p2)
        {
            return Nodes[p1].Position.Distance(Nodes[p2].Position);
        }

    }
}
