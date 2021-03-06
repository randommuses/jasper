﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alba.Stubs;
using JasperHttp.Routing;
using StoryTeller;
using StoryTeller.Grammars.Tables;

namespace StorytellerSpecs.Fixtures.Routing
{
    public class RoutingFixture : Fixture
    {
        private RouteTree _tree;

        public override void SetUp()
        {
            _tree = new RouteTree();
        }

        [ExposeAsTable("If the routes are")]
        public void RoutesAre(string Route)
        {
            var route = new Route(Route, HttpVerbs.GET, _ => Task.CompletedTask);

            _tree.AddRoute(route);
        }

        [ExposeAsTable("The selection and arguments should be")]
        public void TheSelectionShouldBe(string Url, out string Selected, [Default("NONE")]out ArgumentExpectation Arguments)
        {
            var env = StubHttpContext.Empty();
            var leaf = _tree.Select(Url);

            Selected = leaf.Pattern;

            leaf.SetValues(env, RouteTree.ToSegments(Url));

            Arguments = new ArgumentExpectation(env);
        }

    }

    public class ArgumentExpectation
    {
        private readonly string[] _spread;
        private readonly IDictionary<string, object> _args;

        public ArgumentExpectation(string text)
        {
            _spread = new string[0];
            _args = new Dictionary<string, object>();

            if (text == "NONE") return;

            var args = text.Split(';');
            foreach (var arg in args)
            {
                var parts = arg.Trim().Split(':');
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                if (key == "spread")
                {
                    _spread = value == "empty" ? new string[0] : value.Split(',').Select(x => x.Trim()).ToArray();
                }
                else
                {
                    _args.Add(key, value);
                }

            }
        }

        public ArgumentExpectation(StubHttpContext context)
        {
            _spread = context.GetSpreadData();
            _args = context.GetRouteData();
        }

        protected bool Equals(ArgumentExpectation other)
        {
            return _spread.SequenceEqual(other._spread) && _args.SequenceEqual(other._args);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ArgumentExpectation)obj);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            if (_spread.Length == 0 && _args.Count == 0) return "NONE";

            var spreadDescription = "spread: " + string.Join(", ", _spread);

            var argDescription = string.Join("; ", _args.Select(x => $"{x.Key}: {x.Value}").ToArray());


            if (_spread.Length == 0) return argDescription;

            if (_args.Count == 0) return argDescription;

            return $"{argDescription}; {spreadDescription}";
        }
    }
}