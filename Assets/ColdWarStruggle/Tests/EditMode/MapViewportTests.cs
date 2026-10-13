using System;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class MapViewportTests
    {
        static readonly Rect Area=new Rect(20,30,600,400);
        [Test] public void Projection_preserves_aspect_and_bottom_left_coordinates()
        {
            var v=new MapViewport();Assert.That(v.Project(Vector2.zero,Area),Is.EqualTo(new Vector2(20,380)));
            Assert.That(v.Project(Vector2.one,Area),Is.EqualTo(new Vector2(620,80)));
            var p=new Vector2(.21f,.65f);Assert.That(Vector2.Distance(v.Unproject(v.Project(p,Area),Area),p),Is.LessThan(.00001));
            Assert.That(v.Project(new Vector2(.5f,.5f),new Rect(0,0,800,200)),Is.EqualTo(new Vector2(400,100)));
        }
        [Test] public void Zoom_keeps_cursor_anchor_until_world_boundary_clamps()
        {
            var v=new MapViewport();var screen=new Vector2(370,210);var before=v.Unproject(screen,Area);
            v.ZoomAt(3,screen,Area);Assert.That(v.Zoom,Is.EqualTo(3));Assert.That(Vector2.Distance(before,v.Unproject(screen,Area)),Is.LessThan(.00001));
            v.ZoomAt(100,Area.center,Area);Assert.That(v.Zoom,Is.EqualTo(12));v.ZoomAt(.00001f,Area.center,Area);Assert.That(v.Zoom,Is.EqualTo(1));
            Assert.That(v.Center,Is.EqualTo(new Vector2(.5f,.5f)));
        }
        [Test] public void Pan_focus_and_reset_never_expose_unbounded_camera_state()
        {
            var v=new MapViewport();v.Pan(new Vector2(9000,9000),Area);Assert.That(v.Center,Is.EqualTo(new Vector2(.5f,.5f)));
            v.Focus(new Vector2(.7f,.4f),4,Area);Assert.That(v.Center,Is.EqualTo(new Vector2(.7f,.4f)));
            var old=v.Center;v.Pan(new Vector2(30,30),Area);Assert.That(v.Center.x,Is.LessThan(old.x));Assert.That(v.Center.y,Is.GreaterThan(old.y));
            v.Pan(new Vector2(99999,-99999),Area);Assert.That(v.Center.x,Is.EqualTo(.125f).Within(.0001));Assert.That(v.Center.y,Is.EqualTo(1f/6).Within(.0001));
            v.Reset();Assert.That(v.Zoom,Is.EqualTo(1));Assert.That(v.Center,Is.EqualTo(new Vector2(.5f,.5f)));
        }
        [TestCase(0,400)] [TestCase(-1,400)] [TestCase(600,0)] [TestCase(float.NaN,400)]
        public void Invalid_viewport_is_rejected(float width,float height)
        { Assert.That(()=>new MapViewport().Project(Vector2.zero,new Rect(0,0,width,height)),Throws.InstanceOf<ArgumentException>()); }
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(0)] [TestCase(-1)]
        public void Invalid_zoom_does_not_change_camera(float factor)
        {
            var v=new MapViewport();Assert.That(()=>v.ZoomAt(factor,Area.center,Area),Throws.InstanceOf<ArgumentException>());
            Assert.That(v.Zoom,Is.EqualTo(1));Assert.That(v.Center,Is.EqualTo(new Vector2(.5f,.5f)));
        }
    }
}
