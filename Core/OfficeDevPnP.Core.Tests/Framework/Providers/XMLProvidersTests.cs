﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Tests.Framework.Providers.Extensibility;
using System.Security.Cryptography.X509Certificates;
using OfficeDevPnP.Core.Framework.Provisioning.Providers;
using Microsoft.SharePoint.Client;
using System.Threading;
using System.Xml.Linq;
using OfficeDevPnP.Core.Utilities;

namespace OfficeDevPnP.Core.Tests.Framework.Providers
{
    [TestClass]
    public class XMLProvidersTests
    {
        #region Test variables

        static string testContainer = "pnptest";
        static string testContainerSecure = "pnptestsecure";
        static string testTemplatesDocLib = "PnPTemplatesTests";

        private const string TEST_CATEGORY = "Framework Provisioning XML Providers";

        #endregion

        #region Test initialize and cleanup
        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            if (!String.IsNullOrEmpty(TestCommon.AzureStorageKey))
            {
                UploadTemplatesToAzureStorageAccount();
            }

            if (!String.IsNullOrEmpty(TestCommon.DevSiteUrl))
            {
                CleanupTemplatesFromSharePointLibrary();
                UploadTemplatesToSharePointLibrary();
            }
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            if (!String.IsNullOrEmpty(TestCommon.AzureStorageKey))
            {
                CleanupTemplatesFromAzureStorageAccount();
            }

            if (!String.IsNullOrEmpty(TestCommon.DevSiteUrl))
            {
                CleanupTemplatesFromSharePointLibrary();
            }
        }

        private static void CleanupTemplatesFromAzureStorageAccount()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(TestCommon.AzureStorageKey);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(testContainer);
            container.DeleteIfExists();

            CloudBlobContainer containerSecure = blobClient.GetContainerReference(testContainerSecure);
            containerSecure.DeleteIfExists();
        }

        private static void UploadTemplatesToAzureStorageAccount()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(TestCommon.AzureStorageKey);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(testContainer);
            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            // Upload files
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("ProvisioningTemplate-2015-03-Sample-01.xml");
            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = System.IO.File.OpenRead(String.Format(@"{0}\..\..\Resources\Templates\{1}", AppDomain.CurrentDomain.BaseDirectory, "ProvisioningTemplate-2015-03-Sample-01.xml")))
            {
                blockBlob.UploadFromStream(fileStream);
            }

            blockBlob = container.GetBlockBlobReference("ProvisioningTemplate-2015-03-Sample-02.xml");
            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = System.IO.File.OpenRead(String.Format(@"{0}\..\..\Resources\Templates\{1}", AppDomain.CurrentDomain.BaseDirectory, "ProvisioningTemplate-2015-03-Sample-02.xml")))
            {
                blockBlob.UploadFromStream(fileStream);
            }

            CloudBlobContainer containerSecure = blobClient.GetContainerReference(testContainerSecure);
            // Create the container if it doesn't already exist.
            containerSecure.CreateIfNotExists();

            // Avoid public access to this test container
            BlobContainerPermissions bcp = new BlobContainerPermissions();
            bcp.PublicAccess = BlobContainerPublicAccessType.Off;
            containerSecure.SetPermissions(bcp);

            blockBlob = containerSecure.GetBlockBlobReference("ProvisioningTemplate-2015-03-Sample-02.xml");
            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = System.IO.File.OpenRead(String.Format(@"{0}\..\..\Resources\Templates\{1}", AppDomain.CurrentDomain.BaseDirectory, "ProvisioningTemplate-2015-03-Sample-02.xml")))
            {
                blockBlob.UploadFromStream(fileStream);
            }
        }

        private static void UploadTemplatesToSharePointLibrary()
        {
            var context = TestCommon.CreatePnPClientContext();

            var docLib = context.Web.CreateDocumentLibrary(testTemplatesDocLib);
            context.Load(docLib, d => d.RootFolder);
            context.ExecuteQueryRetry();

            var templatesToUpload = new string[] {
                "ProvisioningTemplate-2016-05-Sample-02.xml"
            };

            foreach (var tempFile in templatesToUpload)
            {
                // Create or overwrite the "myblob" blob with contents from a local file.
                using (var fileStream = System.IO.File.OpenRead(String.Format(@"{0}\..\..\Resources\Templates\{1}", AppDomain.CurrentDomain.BaseDirectory, tempFile)))
                {
                    docLib.RootFolder.UploadFile(tempFile, fileStream, true);
                    context.ExecuteQueryRetry();
                }
            }

        }

        private static void CleanupTemplatesFromSharePointLibrary()
        {
            var context = TestCommon.CreatePnPClientContext();

            var docLib = context.Web.GetListByTitle(testTemplatesDocLib);
            if (docLib != null)
            {
                context.Load(docLib);
                context.ExecuteQueryRetry();
                docLib.DeleteObject();
                context.ExecuteQueryRetry();
            }
        }
        #endregion

        #region XML File System tests

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLFileSystemGetTemplatesTest()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var result = provider.GetTemplates();

            Assert.IsTrue(result.Count == 10);
            Assert.IsTrue(result[0].Files.Count == 5);
            Assert.IsTrue(result[1].Files.Count == 5);
            Assert.IsTrue(result[2].Files.Count == 6);
            Assert.IsTrue(result[3].Files.Count == 5);
            Assert.IsTrue(result[4].Files.Count == 1);
            Assert.IsTrue(result[5].Files.Count == 5);
            Assert.IsTrue(result[6].Files.Count == 1);
            Assert.IsTrue(result[7].Files.Count == 1);
            Assert.IsTrue(result[8].Files.Count == 1);
            Assert.IsTrue(result[9].Files.Count == 5);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLFileSystemGetTemplate1Test()
        {
            var _expectedID = "SPECIALTEAM";
            var _expectedVersion = 1.0;

            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var result = provider.GetTemplate("ProvisioningTemplate-2015-03-Sample-01.xml");

            Assert.AreEqual(_expectedID, result.Id);
            Assert.AreEqual(_expectedVersion, result.Version);
            Assert.IsTrue(result.Lists.Count == 1);
            Assert.IsTrue(result.Files.Count == 1);
            Assert.IsTrue(result.SiteFields.Count == 4);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLFileSystemGetTemplate2Test()
        {
            var _expectedID = "SPECIALTEAM";
            var _expectedVersion = 1.0;

            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var result = provider.GetTemplate("ProvisioningTemplate-2015-03-Sample-02.xml");

            Assert.AreEqual(_expectedID, result.Id);
            Assert.AreEqual(_expectedVersion, result.Version);
            Assert.IsTrue(result.Lists.Count == 2);
            Assert.IsTrue(result.Files.Count == 5);
            Assert.IsTrue(result.SiteFields.Count == 4);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLFileSystemGetTemplate3Test()
        {
            var _expectedID = "SPECIALTEAM";
            var _expectedVersion = 1.0;

            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var result = provider.GetTemplate("ProvisioningTemplate-2016-05-Sample-02.xml");

            Assert.AreEqual(_expectedID, result.Id);
            Assert.AreEqual(_expectedVersion, result.Version);
            Assert.IsTrue(result.Lists.Count == 2);
            Assert.IsTrue(result.SiteFields.Count == 4);
        }

        #endregion

        #region XML SharePoint tests

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLSharePointGetTemplate1Test()
        {
            var _expectedID = "SPECIALTEAM";
            var _expectedVersion = 1.0;

            var context = TestCommon.CreatePnPClientContext();

            XMLSharePointTemplateProvider provider =
                new XMLSharePointTemplateProvider(context,
                    TestCommon.DevSiteUrl,
                    testTemplatesDocLib);

            var result = provider.GetTemplate("ProvisioningTemplate-2016-05-Sample-02.xml");

            Assert.AreEqual(_expectedID, result.Id);
            Assert.AreEqual(_expectedVersion, result.Version);
            Assert.IsTrue(result.Lists.Count == 2);
            Assert.IsTrue(result.SiteFields.Count == 4);
        }

        #endregion

        #region XML Azure Storage tests

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLAzureStorageGetTemplatesTest()
        {
            if (String.IsNullOrEmpty(TestCommon.AzureStorageKey))
            {
                Assert.Inconclusive("No Azure Storage Key defined in App.Config, so can't test");
            }

            XMLTemplateProvider provider =
                new XMLAzureStorageTemplateProvider(
                    TestCommon.AzureStorageKey, testContainer);

            var result = provider.GetTemplates();

            Assert.IsTrue(result.Count == 2);
            Assert.IsTrue(result[0].Files.Count == 1);
            Assert.IsTrue(result[1].Files.Count == 5);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLAzureStorageGetTemplate1Test()
        {
            if (String.IsNullOrEmpty(TestCommon.AzureStorageKey))
            {
                Assert.Inconclusive("No Azure Storage Key defined in App.Config, so can't test");
            }

            var _expectedID = "SPECIALTEAM";
            var _expectedVersion = 1.0;

            XMLTemplateProvider provider =
                new XMLAzureStorageTemplateProvider(
                    TestCommon.AzureStorageKey, testContainer);

            var result = provider.GetTemplate("ProvisioningTemplate-2015-03-Sample-01.xml");

            Assert.AreEqual(_expectedID, result.Id);
            Assert.AreEqual(_expectedVersion, result.Version);
            Assert.IsTrue(result.Lists.Count == 1);
            Assert.IsTrue(result.Files.Count == 1);
            Assert.IsTrue(result.SiteFields.Count == 4);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLAzureStorageGetTemplate2SecureTest()
        {
            var _expectedID = "SPECIALTEAM";
            var _expectedVersion = 1.0;

            if (String.IsNullOrEmpty(TestCommon.AzureStorageKey))
            {
                Assert.Inconclusive("No Azure Storage Key defined in App.Config, so can't test");
            }

            XMLTemplateProvider provider =
                new XMLAzureStorageTemplateProvider(
                    TestCommon.AzureStorageKey, testContainerSecure);

            var result = provider.GetTemplate("ProvisioningTemplate-2015-03-Sample-02.xml");

            Assert.AreEqual(_expectedID, result.Id);
            Assert.AreEqual(_expectedVersion, result.Version);
            Assert.IsTrue(result.Lists.Count == 2);
            Assert.IsTrue(result.Files.Count == 5);
            Assert.IsTrue(result.SiteFields.Count == 4);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLFileSystemConvertTemplatesFromV201503toV201605()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var _source1 = provider.GetTemplate("ProvisioningTemplate-2015-03-Sample-01.xml");
            provider.SaveAs(_source1, "ProvisioningTemplate-2016-05-Sample-01.xml", XMLPnPSchemaFormatter.LatestFormatter);

            var _source2 = provider.GetTemplate("ProvisioningTemplate-2015-03-Sample-02.xml");
            provider.SaveAs(_source2, "ProvisioningTemplate-2016-05-Sample-02.xml", XMLPnPSchemaFormatter.LatestFormatter);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void ResolveSchemaFormatV201503()
        {
            var _expectedID = "SPECIALTEAM";
            var _expectedVersion = 1.0;

            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var result = provider.GetTemplate("ProvisioningTemplate-2015-03-Sample-02.xml");

            Assert.AreEqual(_expectedID, result.Id);
            Assert.AreEqual(_expectedVersion, result.Version);
            Assert.IsTrue(result.Lists.Count == 2);
            Assert.IsTrue(result.Files.Count == 5);
            Assert.IsTrue(result.SiteFields.Count == 4);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void ResolveSchemaFormatV201505()
        {
            var _expectedID = "SPECIALTEAM";
            var _expectedVersion = 1.0;

            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var result = provider.GetTemplate("ProvisioningSchema-2015-05-ReferenceSample-01.xml");

            Assert.AreEqual(_expectedID, result.Id);
            Assert.AreEqual(_expectedVersion, result.Version);
            Assert.IsTrue(result.Lists.Count == 2);
            Assert.IsTrue(result.Files.Count == 5);
            Assert.IsTrue(result.SiteFields.Count == 4);
        }

        #endregion

        #region XInclude Tests

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLResolveValidXInclude()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var result = provider.GetTemplate("ProvisioningTemplate-2015-05-Valid-XInclude-01.xml");

            Assert.IsTrue(result.PropertyBagEntries.Count == 2);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLResolveInvalidXInclude()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var result = provider.GetTemplate("ProvisioningTemplate-2015-05-NOT-Valid-XInclude-01.xml");

            Assert.IsTrue(result.PropertyBagEntries.Count == 0);
        }

        #endregion

        #region Provider Extensibility Tests

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLEncryptionTest()
        {
            X509Certificate2 certificate = RetrieveCertificateFromStore(new X509Store(StoreLocation.CurrentUser), "PnPTestCertificate");

            if (certificate == null)
            {
                Assert.Inconclusive("Missing certificate with SN=PnPTestCertificate in CurrentUser Certificate Store, so can't test");
            }

            XMLEncryptionTemplateProviderExtension extension =
                new XMLEncryptionTemplateProviderExtension();

            extension.Initialize(certificate);

            ITemplateProviderExtension[] extensions = new ITemplateProviderExtension[] { extension };

            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var template = provider.GetTemplate("ProvisioningTemplate-2016-05-Sample-01.xml");
            template.DisplayName = "Ciphered template";

            provider.SaveAs(template, "ProvisioningTemplate-2016-05-Ciphered.xml", extensions);
            var result = provider.GetTemplate("ProvisioningTemplate-2016-05-Ciphered.xml", extensions);

            provider.Delete("ProvisioningTemplate-2016-05-Ciphered.xml");

            Assert.IsTrue(result.DisplayName == "Ciphered template");
        }

        private static X509Certificate2 RetrieveCertificateFromStore(X509Store store, String subjectName)
        {
            if (store == null)
                throw new ArgumentNullException("store");

            X509Certificate2 cert = null;

            try
            {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);

                if (certs.Count > 0)
                {
                    // Get the first certificate in the collection
                    cert = certs[0];
                }
            }
            finally
            {
                if (store != null)
                    store.Close();
            }

            return cert;
        }

        #endregion

        #region Formatter Refactoring Tests

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLSerializer_Deserialize_201605()
        {
            var _expectedID = "SPECIALTEAM-01";
            var _expectedVersion = 1.0;

            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var serializer = new XMLPnPSchemaV201605Serializer();
            var result = provider.GetTemplate("ProvisioningTemplate-2016-05-Sample-03.xml", serializer);

            Assert.IsNotNull(result);
            Assert.AreEqual(_expectedID, result.Id);
            Assert.AreEqual(_expectedVersion, result.Version);
            Assert.IsTrue(result.Lists.Count == 1);

            //content types asserts
            Assert.IsNotNull(result.ContentTypes);
            Assert.AreEqual(1, result.ContentTypes.Count);
            Assert.IsNotNull(result.ContentTypes[0].FieldRefs);
            Assert.AreEqual(4, result.ContentTypes[0].FieldRefs.Count);
            Assert.AreEqual(1, result.ContentTypes[0].FieldRefs.Count(f => f.Required));

            Assert.IsTrue(result.PropertyBagEntries.Count == 2);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLSerializer_Serialize_201605()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var serializer = new XMLPnPSchemaV201605Serializer();
            var result = provider.GetTemplate("ProvisioningTemplate-2016-05-Sample-03.xml", serializer);

            provider.SaveAs(result, "ProvisioningTemplate-2016-05-Sample-03-OUT.xml", serializer);

            Assert.IsTrue(System.IO.File.Exists($"{provider.Connector.Parameters["ConnectionString"]}\\{provider.Connector.Parameters["Container"]}\\ProvisioningTemplate-2016-05-Sample-03-OUT.xml"));
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLSerializer_SerializeDeserialize_201605()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var serializer = new XMLPnPSchemaV201605Serializer();
            var template1 = provider.GetTemplate("ProvisioningTemplate-2016-05-Sample-03.xml", serializer);
            Assert.IsNotNull(template1);

            provider.SaveAs(template1, "ProvisioningTemplate-2016-05-Sample-03-OUT.xml", serializer);
            Assert.IsTrue(System.IO.File.Exists($"{provider.Connector.Parameters["ConnectionString"]}\\{provider.Connector.Parameters["Container"]}\\ProvisioningTemplate-2016-05-Sample-03-OUT.xml"));

            var template2 = provider.GetTemplate("ProvisioningTemplate-2016-05-Sample-03-OUT.xml", serializer);
            Assert.IsNotNull(template2);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLSerializer_Deserialize_ContentTypes_201605()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var serializer = new XMLPnPSchemaV201605Serializer();
            var result = provider.GetTemplate("ProvisioningTemplate-2016-05-Sample-03.xml", serializer);

            Assert.IsNotNull(result.ContentTypes);

            var ct = result.ContentTypes.FirstOrDefault();
            Assert.IsNotNull(ct);
            Assert.AreEqual("0x01005D4F34E4BE7F4B6892AEBE088EDD215E", ct.Id);
            Assert.AreEqual("General Project Document", ct.Name);
            Assert.AreEqual("General Project Document Content Type", ct.Description);
            Assert.AreEqual("Base Foundation Content Types", ct.Group);
            Assert.AreEqual("/Forms/DisplayForm.aspx", ct.DisplayFormUrl);
            Assert.AreEqual("/Forms/NewForm.aspx", ct.NewFormUrl);
            Assert.AreEqual("/Forms/EditForm.aspx", ct.EditFormUrl);
            Assert.AreEqual("DocumentTemplate.dotx", ct.DocumentTemplate);
            Assert.IsTrue(ct.Hidden);
            Assert.IsTrue(ct.Overwrite);
            Assert.IsTrue(ct.ReadOnly);
            Assert.IsTrue(ct.Sealed);

            Assert.IsNotNull(ct.DocumentSetTemplate);
            Assert.IsNotNull(ct.DocumentSetTemplate.AllowedContentTypes);
            Assert.IsNotNull(ct.DocumentSetTemplate.AllowedContentTypes.FirstOrDefault(c => c == "0x01005D4F34E4BE7F4B6892AEBE088EDD215E002"));
            Assert.AreNotEqual(Guid.Empty, ct.DocumentSetTemplate.SharedFields.FirstOrDefault(c => c == new Guid("f6e7bdd5-bdcb-4c72-9f18-2bd8c27003d3")));
            Assert.AreNotEqual(Guid.Empty, ct.DocumentSetTemplate.SharedFields.FirstOrDefault(c => c == new Guid("a8df65ec-0d06-4df1-8edf-55d48b3936dc")));
            Assert.AreNotEqual(Guid.Empty, ct.DocumentSetTemplate.WelcomePageFields.FirstOrDefault(c => c == new Guid("c69d2ffc-0c86-474a-9cc7-dcd7774da531")));
            Assert.AreNotEqual(Guid.Empty, ct.DocumentSetTemplate.WelcomePageFields.FirstOrDefault(c => c == new Guid("b9132b30-2b9e-47d4-b0fc-1ac34a61506f")));
            Assert.AreEqual("home.aspx", ct.DocumentSetTemplate.WelcomePage);
            Assert.IsNotNull(ct.DocumentSetTemplate.DefaultDocuments);



            var dd = ct.DocumentSetTemplate.DefaultDocuments.FirstOrDefault(d => d.ContentTypeId == "0x01005D4F34E4BE7F4B6892AEBE088EDD215E001");
            Assert.IsNotNull(dd);
            Assert.AreEqual("document.dotx", dd.FileSourcePath);
            Assert.AreEqual("DefaultDocument", dd.Name);

            Assert.IsNotNull(ct.FieldRefs);
            Assert.AreEqual(4, ct.FieldRefs.Count);

            var field = ct.FieldRefs.FirstOrDefault(f => f.Name == "TestField");
            Assert.IsNotNull(field);
            Assert.AreEqual(new Guid("23203e97-3bfe-40cb-afb4-07aa2b86bf45"), field.Id);
            Assert.IsTrue(field.Required);
            Assert.IsTrue(field.Hidden);
        }


        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLSerializer_Serialize_ContentTypes_201605()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var serializer = new XMLPnPSchemaV201605Serializer();
            var result =  new ProvisioningTemplate();

            var nct = new Core.Framework.Provisioning.Model.ContentType()
            {
                Id = "0x01005D4F34E4BE7F4B6892AEBE088EDD215E",
                Name = "General Project Document",
                Description = "General Project Document Content Type",
                Group = "Base Foundation Content Types",
                DisplayFormUrl = "/Forms/DisplayForm.aspx",
                NewFormUrl = "/Forms/NewForm.aspx",
                EditFormUrl = "/Forms/EditForm.aspx",
                DocumentTemplate = "DocumentTemplate.dotx",
                Hidden = true,
                Overwrite = true,
                ReadOnly = true,
                Sealed = true
            };

            var dt = new DocumentSetTemplate();
            dt.AllowedContentTypes.Add("0x01005D4F34E4BE7F4B6892AEBE088EDD215E002");
            dt.SharedFields.Add(new Guid("f6e7bdd5-bdcb-4c72-9f18-2bd8c27003d3"));
            dt.SharedFields.Add(new Guid("a8df65ec-0d06-4df1-8edf-55d48b3936dc"));
            dt.WelcomePageFields.Add(new Guid("c69d2ffc-0c86-474a-9cc7-dcd7774da531"));
            dt.WelcomePageFields.Add(new Guid("b9132b30-2b9e-47d4-b0fc-1ac34a61506f"));
            dt.WelcomePage = "home.aspx";
            dt.DefaultDocuments.Add(new DefaultDocument()
            {
                ContentTypeId = "0x01005D4F34E4BE7F4B6892AEBE088EDD215E001",
                FileSourcePath = "document.dotx",
                Name = "DefaultDocument"
            });
            nct.DocumentSetTemplate = dt;
            nct.FieldRefs.Add(new FieldRef("TestField")
            {
                Id = new Guid("23203e97-3bfe-40cb-afb4-07aa2b86bf45"),
                Required = true,
                Hidden = true
            });
            nct.FieldRefs.Add(new FieldRef("TestField1"));
            nct.FieldRefs.Add(new FieldRef("TestField2"));
            nct.FieldRefs.Add(new FieldRef("TestField3"));
            result.ContentTypes.Add(nct);

            provider.SaveAs(result, "ProvisioningTemplate-2016-05-Sample-03-OUT-ct.xml", serializer);

            var path = $"{provider.Connector.Parameters["ConnectionString"]}\\{provider.Connector.Parameters["Container"]}\\ProvisioningTemplate-2016-05-Sample-03-OUT-ct.xml";
            Assert.IsTrue(System.IO.File.Exists(path));
            XDocument xml = XDocument.Load(path);
            Core.Framework.Provisioning.Providers.Xml.V201605.Provisioning wrappedResult = 
                XMLSerializer.Deserialize<Core.Framework.Provisioning.Providers.Xml.V201605.Provisioning>(xml);

            Assert.IsNotNull(wrappedResult);
            Assert.IsNotNull(wrappedResult.Templates);
            Assert.AreEqual(1, wrappedResult.Templates.Count());
            Assert.IsNotNull(wrappedResult.Templates[0].ProvisioningTemplate);
            Assert.AreEqual(1, wrappedResult.Templates[0].ProvisioningTemplate.Count());

            var template = wrappedResult.Templates[0].ProvisioningTemplate.First();
            Assert.IsNotNull(template.ContentTypes);

            var ct = template.ContentTypes.FirstOrDefault();
            Assert.IsNotNull(ct);

            Assert.AreEqual("0x01005D4F34E4BE7F4B6892AEBE088EDD215E", ct.ID);
            Assert.AreEqual("General Project Document", ct.Name);
            Assert.AreEqual("General Project Document Content Type", ct.Description);
            Assert.AreEqual("Base Foundation Content Types", ct.Group);
            Assert.AreEqual("/Forms/DisplayForm.aspx", ct.DisplayFormUrl);
            Assert.AreEqual("/Forms/NewForm.aspx", ct.NewFormUrl);
            Assert.AreEqual("/Forms/EditForm.aspx", ct.EditFormUrl);
            Assert.IsNotNull(ct.DocumentTemplate);
            Assert.AreEqual("DocumentTemplate.dotx", ct.DocumentTemplate.TargetName);
            Assert.IsTrue(ct.Hidden);
            Assert.IsTrue(ct.Overwrite);
            Assert.IsTrue(ct.ReadOnly);
            Assert.IsTrue(ct.Sealed);

            Assert.IsNotNull(ct.DocumentSetTemplate);
            Assert.IsNotNull(ct.DocumentSetTemplate.AllowedContentTypes);
            Assert.IsNotNull(ct.DocumentSetTemplate.AllowedContentTypes.FirstOrDefault(c => c.ContentTypeID == "0x01005D4F34E4BE7F4B6892AEBE088EDD215E002"));
            Assert.IsNotNull(ct.DocumentSetTemplate.SharedFields.FirstOrDefault(c => c.ID == "f6e7bdd5-bdcb-4c72-9f18-2bd8c27003d3"));
            Assert.IsNotNull(ct.DocumentSetTemplate.SharedFields.FirstOrDefault(c => c.ID == "a8df65ec-0d06-4df1-8edf-55d48b3936dc"));
            Assert.IsNotNull(ct.DocumentSetTemplate.WelcomePageFields.FirstOrDefault(c => c.ID == "c69d2ffc-0c86-474a-9cc7-dcd7774da531"));
            Assert.IsNotNull(ct.DocumentSetTemplate.WelcomePageFields.FirstOrDefault(c => c.ID == "b9132b30-2b9e-47d4-b0fc-1ac34a61506f"));
            Assert.AreEqual("home.aspx", ct.DocumentSetTemplate.WelcomePage);
            Assert.IsNotNull(ct.DocumentSetTemplate.DefaultDocuments);

            var dd = ct.DocumentSetTemplate.DefaultDocuments.FirstOrDefault(d => d.ContentTypeID == "0x01005D4F34E4BE7F4B6892AEBE088EDD215E001");
            Assert.IsNotNull(dd);
            Assert.AreEqual("document.dotx", dd.FileSourcePath);
            Assert.AreEqual("DefaultDocument", dd.Name);

            Assert.IsNotNull(ct.FieldRefs);
            Assert.AreEqual(4, ct.FieldRefs.Count());

            var field = ct.FieldRefs.FirstOrDefault(f => f.Name == "TestField");
            Assert.IsNotNull(field);
            Assert.AreEqual("23203e97-3bfe-40cb-afb4-07aa2b86bf45", field.ID);
            Assert.IsTrue(field.Required);
            Assert.IsTrue(field.Hidden);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLSerializer_Deserialize_CustomActions_201605()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var serializer = new XMLPnPSchemaV201605Serializer();
            var result = provider.GetTemplate("ProvisioningTemplate-2016-05-Sample-03.xml", serializer);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.CustomActions);
            Assert.IsNotNull(result.CustomActions.SiteCustomActions);
            Assert.IsNotNull(result.CustomActions.WebCustomActions);

            var ca = result.CustomActions.SiteCustomActions.FirstOrDefault(c => c.Name == "CA_SITE_SETTINGS_SITECLASSIFICATION");
            Assert.IsNotNull(ca);
            Assert.AreEqual("Site Classification Application", ca.Description);
            Assert.AreEqual("Microsoft.SharePoint.SiteSettings", ca.Location);
            Assert.AreEqual("Site Classification", ca.Title);
            Assert.AreEqual(1000, ca.Sequence);
            Assert.IsTrue(ca.Rights.Has(PermissionKind.ManageWeb));
            Assert.AreEqual("https://spmanaged.azurewebsites.net/pages/index.aspx?SPHostUrl={0}", ca.Url);
            Assert.AreEqual(UserCustomActionRegistrationType.None, ca.RegistrationType);
            Assert.IsNotNull(ca.CommandUIExtension);
            Assert.AreEqual("http://sharepoint.com", ca.ImageUrl);
            Assert.AreEqual("101", ca.RegistrationId);
            Assert.AreEqual("alert('boo')", ca.ScriptBlock);
            Assert.AreEqual(2, ca.CommandUIExtension.Nodes().Count());

            ca = result.CustomActions.SiteCustomActions.FirstOrDefault(c => c.Name == "CA_SUBSITE_OVERRIDE");
            Assert.IsNotNull(ca);
            Assert.AreEqual("Override new sub-site link", ca.Description);
            Assert.AreEqual("ScriptLink", ca.Location);
            Assert.AreEqual("SubSiteOveride", ca.Title);
            Assert.AreEqual(100, ca.Sequence);
            Assert.AreEqual("~site/PnP_Provisioning_JS/PnP_EmbeddedJS.js", ca.ScriptSrc);
            Assert.AreEqual(UserCustomActionRegistrationType.ContentType, ca.RegistrationType);
            Assert.IsNull(ca.CommandUIExtension);

            ca = result.CustomActions.WebCustomActions.FirstOrDefault(c => c.Name == "CA_WEB_DOCLIB_MENU_SAMPLE");
            Assert.IsNotNull(ca);
            Assert.AreEqual("Document Library Custom Menu", ca.Description);
            Assert.AreEqual("ActionsMenu", ca.Group);
            Assert.AreEqual("Microsoft.SharePoint.StandardMenu", ca.Location);
            Assert.AreEqual("DocLib Custom Menu", ca.Title);
            Assert.AreEqual(100, ca.Sequence);
            Assert.AreEqual("/_layouts/CustomActionsHello.aspx?ActionsMenu", ca.Url);
            Assert.AreEqual(UserCustomActionRegistrationType.None, ca.RegistrationType);
            Assert.IsNull(ca.CommandUIExtension);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLSerializer_Serialize_CustomActions_201605()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var serializer = new XMLPnPSchemaV201605Serializer();
            var result = new ProvisioningTemplate();
            result.CustomActions = new CustomActions();

            var can = new CustomAction() {
                Name = "CA_SITE_SETTINGS_SITECLASSIFICATION",
                Description = "Site Classification Application",
                Location = "Microsoft.SharePoint.SiteSettings",
                Title = "Site Classification",
                Url = "https://spmanaged.azurewebsites.net/pages/index.aspx?SPHostUrl={0}",
                Sequence = 1000,
                RegistrationType = UserCustomActionRegistrationType.None,
                Rights = new BasePermissions(),
                ImageUrl = "http://sharepoint.com",
                RegistrationId = "101",
                ScriptBlock = "alert('boo')",
                CommandUIExtension = XElement.Parse(@"<CommandUIExtension><CommandUIDefinitions>
                <CommandUIDefinition Location=""Ribbon.Documents.Copies.Controls._children"">
                  <Button Sequence = ""15"" TemplateAlias = ""o1"" ToolTipDescription = ""Download all files separately"" ToolTipTitle = ""Download All"" Description = ""Download all files separately"" LabelText = ""Download All"" Image32by32 = ""~sitecollection/SiteAssets/DownloadAll32x32.png"" Image16by16 = ""~sitecollection/SiteAssets/DownloadAll16x16.png"" Command = ""OfficeDevPnP.Cmd.DownloadAll"" Id = ""Ribbon.Documents.Copies.OfficeDevPnPDownloadAll"" />
                </CommandUIDefinition>
                <CommandUIDefinition Location = ""Ribbon.Documents.Copies.Controls._children"">
                  <Button Sequence = ""20"" TemplateAlias = ""o1"" ToolTipDescription = ""Download all files as single Zip archive"" ToolTipTitle = ""Download All as Zip"" Description = ""Download all files as single Zip"" LabelText = ""Download All as Zip"" Image32by32 = ""~sitecollection/SiteAssets/DownloadAllAsZip32x32.png"" Image16by16 = ""~sitecollection/SiteAssets/DownloadAllAsZip16x16.png"" Command = ""OfficeDevPnP.Cmd.DownloadAllAsZip"" Id = ""Ribbon.Documents.Copies.OfficeDevPnPDownloadAllAsZip"" />
                </CommandUIDefinition>
              </CommandUIDefinitions>
              <CommandUIHandlers>
                <CommandUIHandler Command = ""OfficeDevPnP.Cmd.DownloadAll"" EnabledScript = ""javascript:OfficeDevPnP.Core.RibbonManager.isListViewButtonEnabled('DownloadAll');"" CommandAction = ""javascript:OfficeDevPnP.Core.RibbonManager.invokeCommand('DownloadAll');"" />
                <CommandUIHandler Command = ""OfficeDevPnP.Cmd.DownloadAllAsZip"" EnabledScript = ""javascript:OfficeDevPnP.Core.RibbonManager.isListViewButtonEnabled('DownloadAllAsZip');"" CommandAction = ""javascript:OfficeDevPnP.Core.RibbonManager.invokeCommand('DownloadAllAsZip');"" />
              </CommandUIHandlers></CommandUIExtension>")
            };
            can.Rights.Set(PermissionKind.ManageWeb);
            result.CustomActions.SiteCustomActions.Add(can);

            can = new CustomAction()
            {
                Name = "CA_SUBSITE_OVERRIDE",
                Description = "Override new sub-site link",
                Location = "ScriptLink",
                Title  = "SubSiteOveride",
                Sequence = 100,
                ScriptSrc = "~site/PnP_Provisioning_JS/PnP_EmbeddedJS.js",
                RegistrationType = UserCustomActionRegistrationType.ContentType
            };
            result.CustomActions.SiteCustomActions.Add(can);

            can = new CustomAction()
            {
                Name = "CA_WEB_DOCLIB_MENU_SAMPLE",
                Description = "Document Library Custom Menu",
                Group = "ActionsMenu",
                Location = "Microsoft.SharePoint.StandardMenu",
                Title = "DocLib Custom Menu",
                Sequence = 100,
                Url = "/_layouts/CustomActionsHello.aspx?ActionsMenu",
                RegistrationType = UserCustomActionRegistrationType.None
            };
            result.CustomActions.WebCustomActions.Add(can);

            provider.SaveAs(result, "ProvisioningTemplate-2016-05-Sample-03-OUT-ca.xml", serializer);

            var path = $"{provider.Connector.Parameters["ConnectionString"]}\\{provider.Connector.Parameters["Container"]}\\ProvisioningTemplate-2016-05-Sample-03-OUT-ca.xml";
            Assert.IsTrue(System.IO.File.Exists(path));
            XDocument xml = XDocument.Load(path);
            Core.Framework.Provisioning.Providers.Xml.V201605.Provisioning wrappedResult =
                XMLSerializer.Deserialize<Core.Framework.Provisioning.Providers.Xml.V201605.Provisioning>(xml);

            Assert.IsNotNull(wrappedResult);
            Assert.IsNotNull(wrappedResult.Templates);
            Assert.AreEqual(1, wrappedResult.Templates.Count());
            Assert.IsNotNull(wrappedResult.Templates[0].ProvisioningTemplate);
            Assert.AreEqual(1, wrappedResult.Templates[0].ProvisioningTemplate.Count());

            var template = wrappedResult.Templates[0].ProvisioningTemplate.First();

            Assert.IsNotNull(template.CustomActions.SiteCustomActions);
            Assert.IsNotNull(template.CustomActions.WebCustomActions);

            var ca = template.CustomActions.SiteCustomActions.FirstOrDefault(c => c.Name == "CA_SITE_SETTINGS_SITECLASSIFICATION");
            Assert.IsNotNull(ca);
            Assert.AreEqual("Site Classification Application", ca.Description);
            Assert.AreEqual("Microsoft.SharePoint.SiteSettings", ca.Location);
            Assert.AreEqual("Site Classification", ca.Title);
            Assert.AreEqual(1000, ca.Sequence);
            Assert.AreEqual("ManageWeb", ca.Rights);
            Assert.AreEqual("https://spmanaged.azurewebsites.net/pages/index.aspx?SPHostUrl={0}", ca.Url);
            Assert.AreEqual(Core.Framework.Provisioning.Providers.Xml.V201605.RegistrationType.None, ca.RegistrationType);
            Assert.IsNotNull(ca.CommandUIExtension);
            Assert.AreEqual("http://sharepoint.com", ca.ImageUrl);
            Assert.AreEqual("101", ca.RegistrationId);
            Assert.AreEqual("alert('boo')", ca.ScriptBlock);
            Assert.IsNotNull(ca.CommandUIExtension);
            Assert.IsNotNull(ca.CommandUIExtension.Any);
            Assert.AreEqual(2, ca.CommandUIExtension.Any.Length);

            ca = template.CustomActions.SiteCustomActions.FirstOrDefault(c => c.Name == "CA_SUBSITE_OVERRIDE");
            Assert.IsNotNull(ca);
            Assert.AreEqual("Override new sub-site link", ca.Description);
            Assert.AreEqual("ScriptLink", ca.Location);
            Assert.AreEqual("SubSiteOveride", ca.Title);
            Assert.AreEqual(100, ca.Sequence);
            Assert.AreEqual("~site/PnP_Provisioning_JS/PnP_EmbeddedJS.js", ca.ScriptSrc);
            Assert.AreEqual(Core.Framework.Provisioning.Providers.Xml.V201605.RegistrationType.ContentType, ca.RegistrationType);
            Assert.IsNull(ca.CommandUIExtension);

            ca = template.CustomActions.WebCustomActions.FirstOrDefault(c => c.Name == "CA_WEB_DOCLIB_MENU_SAMPLE");
            Assert.IsNotNull(ca);
            Assert.AreEqual("Document Library Custom Menu", ca.Description);
            Assert.AreEqual("ActionsMenu", ca.Group);
            Assert.AreEqual("Microsoft.SharePoint.StandardMenu", ca.Location);
            Assert.AreEqual("DocLib Custom Menu", ca.Title);
            Assert.AreEqual(100, ca.Sequence);
            Assert.AreEqual("/_layouts/CustomActionsHello.aspx?ActionsMenu", ca.Url);
            Assert.AreEqual(Core.Framework.Provisioning.Providers.Xml.V201605.RegistrationType.None, ca.RegistrationType);
            Assert.IsNull(ca.CommandUIExtension);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLSerializer_Deserialize_Files_201605()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var serializer = new XMLPnPSchemaV201605Serializer();
            var result = provider.GetTemplate("ProvisioningTemplate-2016-05-Sample-03.xml", serializer);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Files);

            var file = result.Files.FirstOrDefault(f => f.Src == "/SitePages/home.aspx");
            Assert.IsNotNull(file);
            Assert.AreEqual("SitePages", file.Folder);
            Assert.AreEqual(Core.Framework.Provisioning.Model.FileLevel.Published, file.Level);
            Assert.IsTrue(file.Overwrite);

            Assert.IsNotNull(file.Properties);
            var property = file.Properties.FirstOrDefault(p => p.Key == "MyProperty1");
            Assert.IsNotNull(property);
            Assert.AreEqual("Value1", property.Value);
            property = file.Properties.FirstOrDefault(p => p.Key == "MyProperty2");
            Assert.IsNotNull(property);
            Assert.AreEqual("Value2", property.Value);

            Assert.IsNotNull(file.Security);
            Assert.IsTrue(file.Security.ClearSubscopes);
            Assert.IsTrue(file.Security.CopyRoleAssignments);
            Assert.IsNotNull(file.Security.RoleAssignments);
            var assingment = file.Security.RoleAssignments.FirstOrDefault(r => r.Principal == "admin@sharepoint.com");
            Assert.IsNotNull(assingment);
            Assert.AreEqual("Owner", assingment.RoleDefinition);
            assingment = file.Security.RoleAssignments.FirstOrDefault(r => r.Principal == "dev@sharepoint.com");
            Assert.IsNotNull(assingment);
            Assert.AreEqual("Contributor", assingment.RoleDefinition);

            Assert.IsNotNull(file.WebParts);
            var webpart = file.WebParts.FirstOrDefault(wp => wp.Title == "My Content");
            Assert.IsNotNull(webpart);
            Assert.AreEqual((uint)1, webpart.Order);
            Assert.AreEqual("Main", webpart.Zone);
            Assert.IsNotNull(webpart.Contents);
            Assert.AreEqual("<webPart>[!<![CDATA[web part definition goes here]]></webPart>", webpart.Contents.Trim());

            Assert.IsNotNull(file.WebParts);
            webpart = file.WebParts.FirstOrDefault(wp => wp.Title == "My Editor");
            Assert.IsNotNull(webpart);
            Assert.AreEqual((uint)10, webpart.Order);
            Assert.AreEqual("Left", webpart.Zone);
            Assert.IsNotNull(webpart.Contents);
            Assert.AreEqual("<webPart>[!<![CDATA[web part definition goes here]]></webPart>", webpart.Contents.Trim());

            file = result.Files.FirstOrDefault(f => f.Src == "/Resources/Files/SAMPLE.js");
            Assert.IsNotNull(file);
            Assert.AreEqual("SAMPLE", file.Folder);
            Assert.AreEqual(Core.Framework.Provisioning.Model.FileLevel.Draft, file.Level);
            Assert.IsFalse(file.Overwrite);
        }

        [TestMethod]
        [TestCategory(TEST_CATEGORY)]
        public void XMLSerializer_Serialize_Files_201605()
        {
            XMLTemplateProvider provider =
                new XMLFileSystemTemplateProvider(
                    String.Format(@"{0}\..\..\Resources",
                    AppDomain.CurrentDomain.BaseDirectory),
                    "Templates");

            var result = new ProvisioningTemplate();

            var newfile = new Core.Framework.Provisioning.Model.File()
            {
                Src = "/SitePages/home.aspx",
                Folder = "SitePages",
                Level = Core.Framework.Provisioning.Model.FileLevel.Published,
                Overwrite = true,
                Security = new ObjectSecurity()
                {
                    ClearSubscopes = true,
                    CopyRoleAssignments = true,
                }
            };
            newfile.Properties.Add("MyProperty1", "Value1");
            newfile.Properties.Add("MyProperty2", "Value2");
            newfile.Security.RoleAssignments.Add(new Core.Framework.Provisioning.Model.RoleAssignment() { Principal = "admin@sharepoint.com", RoleDefinition = "Owner" });
            newfile.Security.RoleAssignments.Add(new Core.Framework.Provisioning.Model.RoleAssignment() { Principal = "dev@sharepoint.com", RoleDefinition = "Contributor" });
            newfile.WebParts.Add(new WebPart() { Title = "My Content", Order = 1, Zone = "Main", Contents = "<webPart>[!<![CDATA[web part definition goes here]]></webPart>" });
            newfile.WebParts.Add(new WebPart() { Title = "My Editor", Order = 10, Zone = "Left", Contents = "<webPart>[!<![CDATA[web part definition goes here]]></webPart>" });
            result.Files.Add(newfile);

            newfile = new Core.Framework.Provisioning.Model.File()
            {
                Src= "/Resources/Files/SAMPLE.js",
                Folder = "SAMPLE",
                Level = Core.Framework.Provisioning.Model.FileLevel.Draft,
                Overwrite = false
            };
            result.Files.Add(newfile);

            var serializer = new XMLPnPSchemaV201605Serializer();
            provider.SaveAs(result, "ProvisioningTemplate-2016-05-Sample-03-OUT-files.xml", serializer);

            var path = $"{provider.Connector.Parameters["ConnectionString"]}\\{provider.Connector.Parameters["Container"]}\\ProvisioningTemplate-2016-05-Sample-03-OUT-files.xml";
            Assert.IsTrue(System.IO.File.Exists(path));
            XDocument xml = XDocument.Load(path);
            Core.Framework.Provisioning.Providers.Xml.V201605.Provisioning wrappedResult =
                XMLSerializer.Deserialize<Core.Framework.Provisioning.Providers.Xml.V201605.Provisioning>(xml);

            Assert.IsNotNull(wrappedResult);
            Assert.IsNotNull(wrappedResult.Templates);
            Assert.AreEqual(1, wrappedResult.Templates.Count());
            Assert.IsNotNull(wrappedResult.Templates[0].ProvisioningTemplate);
            Assert.AreEqual(1, wrappedResult.Templates[0].ProvisioningTemplate.Count());

            var template = wrappedResult.Templates[0].ProvisioningTemplate.First();

            Assert.IsNotNull(template.Files);
            Assert.IsNotNull(template.Files.File);
            var file = template.Files.File.FirstOrDefault(f => f.Src == "/SitePages/home.aspx");
            Assert.IsNotNull(file);
            Assert.AreEqual("SitePages", file.Folder);
            Assert.AreEqual(Core.Framework.Provisioning.Providers.Xml.V201605.FileLevel.Published, file.Level);
            Assert.IsTrue(file.Overwrite);

            Assert.IsNotNull(file.Properties);
            var property = file.Properties.FirstOrDefault(p => p.Key == "MyProperty1");
            Assert.IsNotNull(property);
            Assert.AreEqual("Value1", property.Value);
            property = file.Properties.FirstOrDefault(p => p.Key == "MyProperty2");
            Assert.IsNotNull(property);
            Assert.AreEqual("Value2", property.Value);

            Assert.IsNotNull(file.Security);
            Assert.IsNotNull(file.Security.BreakRoleInheritance);
            Assert.IsTrue(file.Security.BreakRoleInheritance.ClearSubscopes);
            Assert.IsTrue(file.Security.BreakRoleInheritance.CopyRoleAssignments);
            Assert.IsNotNull(file.Security.BreakRoleInheritance.RoleAssignment);
            var assingment = file.Security.BreakRoleInheritance.RoleAssignment.FirstOrDefault(r => r.Principal == "admin@sharepoint.com");
            Assert.IsNotNull(assingment);
            Assert.AreEqual("Owner", assingment.RoleDefinition);
            assingment = file.Security.BreakRoleInheritance.RoleAssignment.FirstOrDefault(r => r.Principal == "dev@sharepoint.com");
            Assert.IsNotNull(assingment);
            Assert.AreEqual("Contributor", assingment.RoleDefinition);

            Assert.IsNotNull(file.WebParts);
            var webpart = file.WebParts.FirstOrDefault(wp => wp.Title == "My Content");
            Assert.IsNotNull(webpart);
            Assert.AreEqual(1, webpart.Order);
            Assert.AreEqual("Main", webpart.Zone);
            Assert.IsNotNull(webpart.Contents);
            Assert.AreEqual("<webPart>[!<![CDATA[web part definition goes here]]></webPart>", webpart.Contents.InnerXml);

            Assert.IsNotNull(file.WebParts);
            webpart = file.WebParts.FirstOrDefault(wp => wp.Title == "My Editor");
            Assert.IsNotNull(webpart);
            Assert.AreEqual(10, webpart.Order);
            Assert.AreEqual("Left", webpart.Zone);
            Assert.IsNotNull(webpart.Contents);
            Assert.AreEqual("<webPart>[!<![CDATA[web part definition goes here]]></webPart>", webpart.Contents.InnerXml);

            file = template.Files.File.FirstOrDefault(f => f.Src == "/Resources/Files/SAMPLE.js");
            Assert.IsNotNull(file);
            Assert.AreEqual("SAMPLE", file.Folder);
            Assert.AreEqual(Core.Framework.Provisioning.Providers.Xml.V201605.FileLevel.Draft, file.Level);
            Assert.IsFalse(file.Overwrite);
            Assert.IsNull(file.Properties);
            Assert.IsNull(file.WebParts);
        }
        #endregion
    }
}
