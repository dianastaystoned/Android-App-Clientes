using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Android.Widget;
using Plugin.Media;
using System;
using System.IO;
using Plugin.CurrentActivity;
using Android.Graphics;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;


namespace AppAccesoAndroidStore
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        string Archivo;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            SupportActionBar.Hide();
            CrossCurrentActivity.Current.Init(this, savedInstanceState);
            var Imagen = FindViewById<ImageView>(Resource.Id.imagen);
            var btnAlmacenar = FindViewById<Button>(Resource.Id.btnAlmacenar);
            var txtNombre = FindViewById<EditText>(Resource.Id.txtNombre);
            var txtDomicilio = FindViewById<EditText>(Resource.Id.txtDomicilio);
            var txtTelefono = FindViewById<EditText>(Resource.Id.txtTelefono);
            var txtCorreo = FindViewById<EditText>(Resource.Id.txtCorreo);
            var txtCredito = FindViewById<EditText>(Resource.Id.txtCredito);

            Imagen.Click += async delegate
            {
                await CrossMedia.Current.Initialize();
                var archivo = await CrossMedia.Current.TakePhotoAsync(
                    new Plugin.Media.Abstractions.StoreCameraMediaOptions
                    {
                        Directory = "Image",
                        Name = txtNombre.Text,
                        SaveToAlbum = true,
                        CompressionQuality = 30,
                        CustomPhotoSize = 30,
                        PhotoSize = Plugin.Media.Abstractions.PhotoSize.Medium,
                        DefaultCamera = Plugin.Media.Abstractions.CameraDevice.Front

                    });
                if (archivo == null)
                    return;
                Bitmap bp = BitmapFactory.DecodeStream(archivo.GetStream());
                Archivo = System.IO.Path.Combine(System.Environment.GetFolderPath
                    (System.Environment.SpecialFolder.Personal), txtNombre.Text + " .jpg");
                var stream = new FileStream(Archivo, FileMode.Create);
                bp.Compress(Bitmap.CompressFormat.Jpeg, 30, stream);
                stream.Close();
                Imagen.SetImageBitmap(bp);
            };
            btnAlmacenar.Click += async delegate
            {
                try
                {
                    var CuentaAlmacenamiento = CloudStorageAccount.Parse("");
                    var ClientBlob = CuentaAlmacenamiento.CreateCloudBlobClient();
                    var Carpeta = ClientBlob.GetContainerReference("image");
                    var resourceBlob = Carpeta.GetBlockBlobReference(txtNombre.Text + ".jpg");
                    resourceBlob.Properties.ContentType = "image/jpeg";
                    await resourceBlob.UploadFromFileAsync(Archivo.ToString());
                    var TablaNoSQL = CuentaAlmacenamiento.CreateCloudTableClient();
                    var Coleccion = TablaNoSQL.GetTableReference("cliente");
                    await Coleccion.CreateIfNotExistsAsync();
                    var cliente = new Cliente("Cliente", txtNombre.Text);
                    cliente.Domicilio = txtDomicilio.Text;
                    cliente.Telefono = txtTelefono.Text;
                    cliente.Correo = txtCorreo.Text;
                    cliente.Credito = double.Parse(txtCredito.Text);
                    cliente.Imagen = "" + txtNombre.Text + ".jpg";
                    var Store = TableOperation.Insert(cliente);
                    await Coleccion.ExecuteAsync(Store);
                    Toast.MakeText(this, "Registro almacenado con éxito", ToastLength.Long).Show();
                    txtNombre.Text = "";
                    txtDomicilio.Text = "";
                    txtCredito.Text = "";
                    txtCorreo.Text = "";
                    txtTelefono.Text = "";
                    Imagen.SetImageDrawable(null);
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                    
                }
            };
        }
    
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
    //Se crea una tabla tipo entidad, nos va permitir mapear las variables directamente con la tabla no sql que tenemos en Azure Storage
    public class Cliente : TableEntity
    {
        public Cliente(string Categoria, string Nombre)
        {
            PartitionKey = Categoria;
            RowKey = Nombre;
        }
        public string Domicilio { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public double Credito { get; set; }
        public string Imagen { get; set; }
    }
}