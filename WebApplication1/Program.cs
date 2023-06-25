using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
long maxId;
using (UserContext db = new UserContext())
{
    foreach (var usr in db.Users)
        db.Users.Remove(usr);
    db.SaveChanges();
    var dbusers = db.Users.ToList();
    if (db.Users.Any()) maxId = dbusers.Max(u => u.id);
    else maxId = 0;
    
    // создаем два объекта User
    User user1 = new User(id: ++maxId, Login: "Superman2000", Password: "qwerty", Name: "Tom", Age: 33);
    User user2 = new User(id: ++maxId, Login: "WhiteRabbit", Password: "qwerty", Name: "Alice", Age: 26);
    bool exists12=false;
    string[] strArray = new string[] { user1.Login, user2.Login };
    foreach (var u in dbusers) {
    if(!(u.Login == user1.Login || u.Login == user2.Login)) {
      exists12 = true; }
    }
    // добавляем их в бд
    if (!exists12)
    {
        db.Users.AddRange(user1, user2);
        db.SaveChanges();
    }
}
using (UserContext db = new UserContext())
{
    // получаем объекты из бд и выводим на консоль
    var dbusers = db.Users.ToList();

    Console.WriteLine("Users list:");
    foreach (User u in dbusers)
    {
        Console.WriteLine($"{u.id}.{u.Name}" + (!String.IsNullOrEmpty(u.Lastname) ? $".{u.Lastname}" : "")
            + (u.Age != 0 ? $" - {u.Age}" : ""));
    }
}

app.Run();

var users = new List<User>();

// ПОЛУЧЕНИЕ ПОЛЬЗОВАТЕЛЯ

    app.MapGet("/api/users", handler: () => {
        return Results.Ok(users);
    });
app.MapPost("/api/users/login", handler: (User u) =>
{
    try
    {
        users.Find(x => x.Login == u.Login);
        if (users.Find(x => x.Login == u.Login).Password == u.Password)
            return Results.Ok("login sucseessful");
        else return Results.Forbid();//authenticationSchemes: new List<String> {"Incorrect Password"}
    }
    catch (ArgumentNullException e)
    {
        return Results.BadRequest("User not Found");
    }

});

// Получение пользователя по id

    app.MapGet("/api/user/{ID:long}", handler: (long ID) => {
        return Results.Ok(users.Find(x => x.id == ID));
    });
app.MapPut("/api/user/{ID:long}", handler: (User user) => {
    using(UserContext db = new UserContext()) {
        var finduser = db.Users.SingleOrDefault(u => u.Login == user.Login);
        if(finduser != null) {
            if (finduser == user) return Results.Ok();
            finduser = user with { id = db.Users.Count() + 1 };
            db.SaveChanges();
            return Results.Ok("User Created");
        }
        else {
            var newuser = user with { id = db.Users.Count() + 1 };
            db.Users.Add(newuser) ;
            db.SaveChanges();
            return Results.Accepted("User Created");
        }
    }
});

app.MapPost("/app/products/add", handler: (Product p) => {
        using (ProductContext db = new ProductContext()) {
            var dbproducts = db.Products.ToList();
            var maxId = dbproducts.Count;
            bool exists = false;
            foreach (var product in dbproducts) {if (product.id == p.id) {exists=true;}}
            if (!exists) { db.Products.Add(p);
                db.SaveChanges();
                return Results.Ok();
            }
            return Results.Forbid();
        } });




    app.MapGet("/app/products/get{id:long}", handler: (long id) =>{
        using (ProductContext db = new ProductContext())    {
            var dbproducts = db.Products.ToList();
            try {
                var product = dbproducts.Find(p => p.id == id);
                return Results.Ok(product);
            }
            catch(ArgumentNullException e) {
                return Results.NotFound(); 
            }
        }
    });
app.MapGet("/app/products/getlist", handler: () => {
    using (ProductContext db = new ProductContext()) {
        var dbproducts = db.Products.ToList();
        return Results.Ok(dbproducts);
    }
});
app.MapPut("/app/products/set{id:long}", handler: (Product product) => {
    using (ProductContext db = new ProductContext()) {
        //       var dbproducts = db.Products.ToList();
        var findproduct = db.Products.SingleOrDefault(p => p.id == product.id);
//        try {
//           var findproduct = dbproducts.Find(p => p.id == product.id);
        if(findproduct != null) {
            if (findproduct == product)
                return Results.Ok();
            else {
                findproduct = product;
                db.SaveChanges();
                return Results.Accepted();
            }           
        }
        return Results.NotFound();
        //        }
        //        catch (ArgumentNullException e) {
        //        }
    }
});
//
public enum ProductType { 
    Brick,
    Cement,
    MetalProfile,
    RoofMetalTile,
    RoofSoftTile,
    Insulation,
    Pipe,
    WallPaper,
    Linoleum,
    Laminate
}

public record User(long id, string Login, string Password, string Name, string? Lastname="",int? Age=0);
public record Product(long id, string Name,DateTime Arrive, uint? Count = 1, uint? Weight = 0);
public class UserContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public UserContext() { Database.EnsureCreated(); }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=usersdb;Username=postgres;Password=N2Nk386");
    }
}
public class ProductContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public ProductContext() { Database.EnsureCreated(); }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=productsdb;Username=postgres;Password=N2Nk386");
    }
}