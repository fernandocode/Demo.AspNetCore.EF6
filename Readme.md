# Using Entity Framework 6 with ASP.NET Core 1.0

*Updated for ASP.NET Core 1.0 RTM based on .NET CLI.*

1. Start with a new C# web app using ASP.NET Core 1.0
    - Select the Web API template (preview)

2. Add Entity Framework under dependencies in the project.json file.
    - This should be the full EF 6.x.

3.  Add a `DbConfig` class that extends `DbConfiguration`

    ```csharp
    public class DbConfig : DbConfiguration
    {
        public DbConfig()
        {
            SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
        }
    }
    ```

4. Add a `Product` entity class.

    ```csharp
    public class Product
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
    }
    ```

5. Add a `SampleDbContext` inheriting from `DbContext`.
    - Place a `DbConfigurationType` attribute on it with `DbConfig`.
    - Add a `Products` property of type `DbSet<Product>`

    ```chsarp
    [DbConfigurationType(typeof(DbConfig))]
    public class SampleDbContext : DbContext
    {
        public SampleDbContext(string connectionName) :
            base(connectionName) { }

        public DbSet<Product> Products { get; set; }
    }
    ```

6. Optionally create a `SampleDbInitializer` class which inherits from `DropCreateDatabaseIfModelChanges<SampleDbContext>`.
    - Override the `Seed` method to see the database with data.

    ```csharp
    public class SampleDbInitializer : DropCreateDatabaseIfModelChanges<SampleDbContext>
    {
        protected override void Seed(SampleDbContext context)
        {
            var products = new List<Product>
            {
                new Product { Id = 1, ProductName = "Chai", UnitPrice = 10 },
                new Product { Id = 2, ProductName = "Chang", UnitPrice = 11 },
                new Product { Id = 3, ProductName = "Aniseed Syrup", UnitPrice = 12 },
            };

            context.Products.AddRange(products);

            context.SaveChanges();
        }
    }    
    ```

7. Add a static ctor to `SampleDbContext` to set the context initializer.

    ```csharp
    static SampleDbContext()
    {
        Database.SetInitializer(new SampleDbInitializer());
    }    
    ```

8. Add a "Data" section to appsettings.json with a connection string
    - Here we specify LocalDb, but SQL Express or full is OK too.

    ```json
    "Data": {
      "SampleDb": {
        "ConnectionString": "Data Source=(localdb)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\SampleDb.mdf;Integrated Security=True; MultipleActiveResultSets=True"
      }
    }
    ```

9. Update the `Startup` ctor to set the "DataDirectory" for the current `AppDomain`.

    ```csharp
    public Startup(IHostingEnvironment env)
    {
        // Set up configuration sources.
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();
        Configuration = builder.Build();

        // Set up data directory
        AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(env.ContentRootPath, "App_Data"));
    }
    ```

10. Register `SampleDbContext` with DI system by supplying a new instance of `SampleDbContext`
    - Add the following code to the `ConfigureServices` method in `Startup`

    ```csharp
    services.AddScoped(provider =>
    {
        var connectionString = Configuration["Data:SampleDb:ConnectionString"];
        return new SampleDbContext(connectionString);
    });
    ```

11. Add a `ProductsController` that extends `Controller`
    - Pass `SampleDbContext` to the ctor
    - Add actions for GET, POST, PUT and DELETE
    - Override `Dispose` to dispose of the context

    ```csharp
    [Route("api/[controller]")]
    public class ProductsController : Controller
    {
        private readonly SampleDbContext _dbContext;

        public ProductsController(SampleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ObjectResult> Get()
        {
            var products = await _dbContext.Products
                .OrderBy(e => e.ProductName)
                .ToListAsync();
            return Ok(products);
        }

        // GET api/products/5
        [HttpGet("{id}")]
        public async Task<ObjectResult> Get(int id)
        {
            var product = await _dbContext.Products
                .SingleOrDefaultAsync(e => e.Id == id);
            return Ok(product);
        }

        // POST api/products
        [HttpPost]
        public async Task<ObjectResult> Post([FromBody]Product product)
        {
            _dbContext.Entry(product).State = EntityState.Added;

            await _dbContext.SaveChangesAsync();
            return Ok(product);
        }

        // PUT api/products/5
        [HttpPut]
        public async Task<ObjectResult> Put([FromBody]Product product)
        {
            _dbContext.Entry(product).State = EntityState.Modified;

            await _dbContext.SaveChangesAsync();
            return Ok(product);
        }

        // DELETE api/products/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var product = await _dbContext.Products
                .SingleOrDefaultAsync(e => e.Id == id);
            if (product == null) return Ok();

            _dbContext.Entry(product).State = EntityState.Deleted;
            await _dbContext.SaveChangesAsync();
            return Ok();
        }
    }
    ```

13. Test the controller by running the app with Ctrl + F5 and submitting some requests.
    - Use Postman or Fiddler
    - Set Content-Type header to application/json for POST and PUT.
    - The database should be created automatically

    ```
    GET: http://localhost:49951/api/products
    POST: http://localhost:49951/api/products
      - Body: {"productName":"Ikura","unitPrice":12}
    GET: http://localhost:49951/api/products/4
    PUT: http://localhost:49951/api/products
      - Body: {"id":4,"productName":"Ikura","unitPrice":13}
    DELETE: http://localhost:49951/api/products/4
    ```

