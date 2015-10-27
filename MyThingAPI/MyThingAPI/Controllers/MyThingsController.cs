using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using MyThingAPI.Models;

namespace MyThingAPI.Controllers
{
    public class MyThingsController : ApiController
    {
        private MyThingAPIContext db = new MyThingAPIContext();

        // GET: api/MyThings
        public IQueryable<MyThing> GetMyThings()
        {
            return db.MyThings;
        }

        // GET: api/MyThings/5
        [ResponseType(typeof(MyThing))]
        public IHttpActionResult GetMyThing(int id)
        {
            MyThing myThing = db.MyThings.Find(id);
            if (myThing == null)
            {
                return NotFound();
            }

            return Ok(myThing);
        }

        // PUT: api/MyThings/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutMyThing(int id, MyThing myThing)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != myThing.id)
            {
                return BadRequest();
            }

            db.Entry(myThing).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MyThingExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/MyThings
        [ResponseType(typeof(MyThing))]
        public IHttpActionResult PostMyThing(MyThing myThing)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.MyThings.Add(myThing);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = myThing.id }, myThing);
        }

        // DELETE: api/MyThings/5
        [ResponseType(typeof(MyThing))]
        public IHttpActionResult DeleteMyThing(int id)
        {
            MyThing myThing = db.MyThings.Find(id);
            if (myThing == null)
            {
                return NotFound();
            }

            db.MyThings.Remove(myThing);
            db.SaveChanges();

            return Ok(myThing);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool MyThingExists(int id)
        {
            return db.MyThings.Count(e => e.id == id) > 0;
        }
    }
}