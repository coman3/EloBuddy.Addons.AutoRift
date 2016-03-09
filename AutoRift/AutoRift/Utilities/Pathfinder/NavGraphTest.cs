﻿using System;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace AutoRift.Utilities.Pathfinder
{
    internal class NavGraphTest
    {
        private readonly NavGraph _navGraph;
        private int _selectedNode = -1;
        private List<Vector3> _p;
        private bool _drawon;
        public NavGraphTest(NavGraph n)
        {
            _p=new List<Vector3>();
            _navGraph = n;
            Menu menu = MainMenu.AddMenu("AB NavGraph", "abnavgraph");
            KeyBind addSelectNode = new KeyBind("Add/select node", false, KeyBind.BindTypes.HoldActive);
            addSelectNode.OnValueChange += addSelectNode_OnValueChange;
            KeyBind removeNode = new KeyBind("remove node", false, KeyBind.BindTypes.HoldActive);
            removeNode.OnValueChange += removeNode_OnValueChange;
            KeyBind addremoveneighbor = new KeyBind("Add/remove neighbor", false, KeyBind.BindTypes.HoldActive);
            addremoveneighbor.OnValueChange += addremoveneighbor_OnValueChange;

            menu.Add("addselsect", addSelectNode);
            menu.Add("removeno", removeNode);
            menu.Add("addneigh", addremoveneighbor);
            Slider zoom=new Slider("Zoom", 2250, 0, 5000);
            menu.Add("zoom", zoom);
            zoom.CurrentValue = (int)Camera.ZoomDistance;
            zoom.OnValueChange += zoom_OnValueChange;
            Chat.OnInput += Chat_OnInput;
            
            
        }

        void Game_OnTick(EventArgs args)
        {

            if (_p.Count == 0) Game.OnTick -= Game_OnTick;
            Player.IssueOrder(GameObjectOrder.MoveTo, _p[0], true);
            if(ObjectManager.Player.Distance(_p[0])<400)
                _p.RemoveAt(0);
        }

        void zoom_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            Camera.SetZoomDistance(args.NewValue);
        }

        void Chat_OnInput(ChatInputEventArgs args)
        {
            if (args.Input.Equals("/ng path"))
            {
                args.Process = false;
                _p = _navGraph.FindPath2(ObjectManager.Player.Position, Game.CursorPos);
            }
            if (args.Input.Equals("/ng walk"))
            {
                args.Process = false;
                _p = _navGraph.FindPath2(ObjectManager.Player.Position, Game.CursorPos);
                Game.OnTick += Game_OnTick;
            }
            if (args.Input.Equals("/ng save"))
            {
                args.Process = false;
                _navGraph.Save();
            }
            if (args.Input.Equals("/ng load"))
            {
                args.Process = false;
                _selectedNode = -1;
                _p = new List<Vector3>();
                _navGraph.Load();
            }
            if (args.Input.Equals("/ng clear"))
            {
                args.Process = false;
                _navGraph.Nodes=new Node[0];
                _p = new List<Vector3>();
                _selectedNode = -1;
            }
            if (args.Input.Equals("/ng show"))
            {
                args.Process = false;
                if(_drawon) return;
                Drawing.OnDraw += Drawing_OnDraw;
                _drawon = true;
            }
            if (args.Input.Equals("/ng hide"))
            {
                args.Process = false;
                Drawing.OnDraw-=Drawing_OnDraw;
                _drawon = false;
            }

        }

        void Drawing_OnDraw(EventArgs args)
        {
            
            if(_selectedNode>=0)
                Circle.Draw(new ColorBGRA(255, 0, 0, 255), 100, _navGraph.Nodes[_selectedNode].Position);
            _navGraph.Draw();

            for (int i = 0; i < _p.Count-1; i++)
            {
                if(_p[i].IsOnScreen()||_p[i+1].IsOnScreen())
                    Line.DrawLine(Color.Aqua, 4, _p[i], _p[i+1]);
            }
        }

        private void removeNode_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue || _selectedNode < 0) return;
            if (_selectedNode >= 0)
            {
                _navGraph.RemoveNode(_selectedNode);
                _selectedNode = _navGraph.FindClosestNode(Game.CursorPos);
            }
        }

        void addremoveneighbor_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue || _selectedNode < 0) return;
            int closestNodeId = _navGraph.FindClosestNode(Game.CursorPos, _selectedNode);
            if (_navGraph.Nodes[closestNodeId].Position.Distance(Game.CursorPos) > 300) return;
            if (_navGraph.LinkExists(_selectedNode, closestNodeId))
                _navGraph.RemoveLink(_selectedNode, closestNodeId);
            else
            {
                _navGraph.AddLink(_selectedNode, closestNodeId);
                _selectedNode = closestNodeId;
            }

        }

        void addSelectNode_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue) return;
            int closestNodeId = _navGraph.FindClosestNode(Game.CursorPos);
            if (closestNodeId==-1||_navGraph.Nodes[closestNodeId].Position.Distance(Game.CursorPos) > 300)
            {
                _navGraph.AddNode(Game.CursorPos);
                _selectedNode = _navGraph.Nodes.Length - 1;
            }
            else
                _selectedNode = closestNodeId;
        }


    }
}
