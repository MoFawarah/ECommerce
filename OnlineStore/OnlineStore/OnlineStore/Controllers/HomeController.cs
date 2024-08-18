using OnlineStore.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace OnlineStore.Controllers
{
    public class HomeController : Controller
    {
        OnlineStoreEntities db = new OnlineStoreEntities();

        public ActionResult Index()
        {

            var listOfCategories = db.categories.ToList();

            return View(listOfCategories);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Register()
        {
            return View();
        }


        [HttpPost]
        public ActionResult Register(user newUser)
        {
            if (newUser == null || !ModelState.IsValid)
            {

                return View();
            }

            var newRecord = db.users.Add(newUser);
            newUser.type = "customer";
            db.SaveChanges();

            return RedirectToAction("Login");

        }


        public ActionResult Login()
        {

            return View();

        }


        [HttpPost]

        public ActionResult Login(user NewUser)
        {
            var userToLogin = db.users.FirstOrDefault(p => p.email == NewUser.email);

            if (userToLogin == null)
            {
                ViewBag.ErrorMessage = "User not found.";
                return View("Login");
            }

            if (userToLogin.password == NewUser.password)
            {
                Session["loggedInUser"] = userToLogin.id;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View("Login");
            }
        }


        public ActionResult ShowProducts(int? id)
        {

            if (id == null)
            {
                var products = db.products.ToList();
                return View(products);
            }

            else
            {



                var productsOfCategory = db.products.Where(p => p.category_id == id).ToList();

                return View(productsOfCategory);
            }


        }

        public ActionResult ProductDetails(int? id)
        {

            var product = db.products.FirstOrDefault(p => p.id == id);


            return View(product);

        }


        public ActionResult AddToCart(int id, int quantity = 1)
        {
            var user_id = (int?)Session["loggedInUser"];

            if (user_id == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var product = db.products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            var userCart = db.carts.FirstOrDefault(c => c.user_id == user_id);
            if (userCart == null)
            {
                userCart = new cart { user_id = user_id.Value };
                db.carts.Add(userCart);
                db.SaveChanges();
            }

            var existingCartItem = userCart.cart_item.FirstOrDefault(ci => ci.product_id == product.id);
            if (existingCartItem != null)
            {
                existingCartItem.quantity += quantity;
            }
            else
            {
                cart_item new_product = new cart_item
                {
                    product_id = product.id,
                    quantity = quantity,
                    cart_id = userCart.id,
                };

                userCart.cart_item.Add(new_product);
            }

            db.SaveChanges();

            Session["CartItems"] = userCart.cart_item.Sum(ci => ci.quantity);


            // Pass the cart item to the view
            var addedItem = userCart.cart_item.FirstOrDefault(ci => ci.product_id == product.id);
            return RedirectToAction("Cart"); // Return the AddToCart view
        }


        public ActionResult Cart()
        {
            var user_id = (int?)Session["loggedInUser"];

            if (user_id == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var userCart = db.carts.Include("cart_item.product").FirstOrDefault(c => c.user_id == user_id);

            if (userCart == null || !userCart.cart_item.Any())
            {
                ViewBag.Message = "Your cart is empty.";
                return View();
            }

            return View(userCart);
        }


        public ActionResult UpdateQuantity(int id, int change)
        {
            var cartItem = db.cart_item.FirstOrDefault(ci => ci.id == id);
            if (cartItem != null)
            {
                cartItem.quantity += change;

                // If the quantity goes below 1, remove the item from the cart
                if (cartItem.quantity <= 0)
                {
                    db.cart_item.Remove(cartItem);
                }

                db.SaveChanges();

                var userCart = db.carts.Include("cart_item").FirstOrDefault(c => c.id == cartItem.cart_id);
                Session["CartItems"] = userCart.cart_item.Sum(ci => ci.quantity);
            }

            return RedirectToAction("Cart");
        }

        public ActionResult RemoveFromCart(int id)
        {
            var cartItem = db.cart_item.FirstOrDefault(ci => ci.id == id);
            if (cartItem != null)
            {
                db.cart_item.Remove(cartItem);
                db.SaveChanges();

                var userCart = db.carts.Include("cart_item").FirstOrDefault(c => c.id == cartItem.cart_id);
                Session["CartItems"] = userCart.cart_item.Sum(ci => ci.quantity);
            }

            return RedirectToAction("Cart");
        }

        public ActionResult Details()
        {

            var userID = (int?)Session["loggedInUser"];

            if (userID == null)
            {
                return RedirectToAction("Login");
            }

            user user = db.users.Find(userID);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        public ActionResult Edit()
        {

            var userID = (int?)Session["loggedInUser"];
            if (userID == null)
            {
                return RedirectToAction("Login");
            }
            user user = db.users.Find(userID);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "id,name,email,password,type")] user user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details");
            }
            return View(user);
        }

        public ActionResult EditPassword()
        {

            return View();
        }

        [HttpPost]
        public ActionResult EditPassword(string oldPassword, string newPassword, string repeatPassword)
        {
            var userID = (int?)Session["loggedInUser"];

            if (userID == null)
            {
                return RedirectToAction("Login");
            }

            user user = db.users.Find(userID);
            if (user == null)
            {
                return HttpNotFound();
            }

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(repeatPassword))
            {
                ViewBag.ErrorMessage = "Please fill in all the fields.";
                return View();
            }

            if (user.password != oldPassword)
            {
                ViewBag.ErrorMessage = "Incorrect old password.";
                return View();
            }

            if (newPassword != repeatPassword)
            {
                ViewBag.ErrorMessage = "New password and repeat password do not match.";
                return View();
            }

            user.password = newPassword;
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            ViewBag.SuccessMessage = "Password successfully changed.";
            return View();
        }

        public ActionResult Logout()
        {
            Session["loggedInUser"] = null;
            return RedirectToAction("Index", "Home");
        }


    }

}