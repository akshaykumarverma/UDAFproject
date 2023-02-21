trigger BusinessLicenseApplicationTrigger on BusinessLicenseApplication (after insert) {
    
    boolean IsAccountPresent = false;
    list<id> setBLAId = new list<id>();
    string accountName;
    id blaAccountId ;
    boolean newYearCal = false;
    
    //public static map<string,GoogleDriveStructure__mdt> mapstrMetaDate = new map<string,GoogleDriveStructure__mdt>();


    
    for(BusinessLicenseApplication obj : trigger.new){
        if(obj.AccountId != null ){
            IsAccountPresent = true;
            blaAccountId = obj.AccountId;
            setBLAId.add(obj.Id);
        }
        //        BusinessLicenseApplicationHandler.handleAfterInsert(obj.Id);
    }
    if(IsAccountPresent){
        
        set<id> setLicenseTypeId = new set<id>(); 
        list<BusinessLicenseApplication> lstBLA =[select id,name,LicenseType.Name,Account.Name, LicenseType.id from BusinessLicenseApplication where Id IN : setBLAId];
        system.debug('lstBLA'+lstBLA);
        
        for(BusinessLicenseApplication  objBLA :lstBLA){
            accountName =objBLA.Account.Name; 
            system.debug('accountName'+accountName);
            setLicenseTypeId.add(objBLA.LicenseTypeId);
        }
        system.debug('setLicenseTypeId---'+setLicenseTypeId);
        
        list<RegulatoryAuthorizationType> lstRAT = [select id,Name ,IssuingDepartment.Name from RegulatoryAuthorizationType where id In : setLicenseTypeId];
        system.debug(lstRAT);
        for(RegulatoryAuthorizationType objRAT : lstRAT){
            
            system.debug('IssuingDepartment.Name'+objRAT.IssuingDepartment.Name);
            if(objRAT.IssuingDepartment.Name == 'Organics'){
                system.debug('InisdeOrganics ');
                boolean flag = checkFolderPresentOrNotInGd(setBLAId,blaAccountId);
                if(flag == true){
                    system.debug('Folder is alredy there');
                    mapExistingFolderToBLA(setBLAId);
                }
                else{
                    system.debug('Folder not there in google drive ');
                    system.debug('newYearCal--------'+newYearCal);
                   CreatingFolderStructureUsingMetaData queueableInstance = new CreatingFolderStructureUsingMetaData(setBLAId,accountName,blaAccountId,newYearCal);
                   Id jobId = System.enqueueJob(queueableInstance); 
                    system.debug('-----jobId------'+jobId);
                }
                
            }
            else if (objRAT.IssuingDepartment.Name =='Hemp'){
                
                boolean flag = checkFolderPresentOrNotInGd(setBLAId,blaAccountId);
                if(flag == true){
                    system.debug('Folder is alredy there');
                    mapExistingFolderToBLA(setBLAId);
                }
                else{
                    system.debug('Folder not there in google drive ');
                    system.debug('Trigger setBLAId-----'+setBLAId);
                    system.debug('Trigger accountName-----'+accountName);
                    //CreatingFolderStructureUsingMetaData queueableInstance = new CreatingFolderStructureUsingMetaData(setBLAId,accountName,blaAccountId);
                   // Id jobId = System.enqueueJob(queueableInstance); 
                   // system.debug('-----jobId------'+jobId);
                }    
                
            }
            else if (objRAT.IssuingDepartment.Name =='Cannabis'){
                
                boolean flag = checkFolderPresentOrNotInGd(setBLAId,blaAccountId);
                if(flag == true){
                    system.debug('Folder is alredy there');
					mapExistingFolderToBLA(setBLAId);           
                }
                else{
                    system.debug('Folder not there in google drive ');
                   // CreatingFolderStructureUsingMetaData queueableInstance = new CreatingFolderStructureUsingMetaData(setBLAId,accountName,blaAccountId);
                   // Id jobId = System.enqueueJob(queueableInstance); 
                   // system.debug('-----jobId------'+jobId);
                    //sendingFiletoFolder(setBLAId);
                }    
                
            }
            else{
            }
        }
    }  
    
    
    
    
    public static boolean checkFolderPresentOrNotInGd(list<id> lstBLAId,id blaAccountId ){
        List<GoogleDrive_Folder__c> lstGD = [select Id,Name,FolderId__c from GoogleDrive_Folder__c where Business_License_Application__r.Id IN:lstBLAId or Account__c =: blaAccountId];
        system.debug('lstGD---'+lstGD);
        //String formattedDate = DateTime.now().format('dd MMM yyyy');
        String formattedDate = string.valueOf(System.Today().year()-1);
        //2022 == 2022
        for(GoogleDrive_Folder__c objGD :lstGD){
            if(objGD.Name == formattedDate){
                newYearCal = true;
                return false;
            }
        }
        if(lstGD.size() > 0){
            return true;
            
        }
        else{
            return false;
        }
        
        
        
        
      /*  if(lstGD.size() > 0){
            return true;
            
        }
        else{
            return false;
        }*/
        
        
    }
    public void mapExistingFolderToBLA(list<id> setBLAId){
        system.debug('Inisde mapExistingFolderToBLA'+blaAccountId);
        List<GoogleDrive_Folder__c> GoogleDriveList = [select Name,CompanyName__c,FolderUrl__c,FolderId__c,Account__c from GoogleDrive_Folder__c where 
                                                       Account__c =: blaAccountId];
        system.debug('GoogleDriveList----'+GoogleDriveList);
        List<GoogleDrive_Folder__c> googleDriveListToInsert = new List<GoogleDrive_Folder__c>();
        for(GoogleDrive_Folder__c gdLst:GoogleDriveList) {
            GoogleDrive_Folder__c gdFobj = new GoogleDrive_Folder__c();
            gdFobj.Name = gdLst.Name;
            gdFobj.FolderUrl__c=gdLst.FolderUrl__c;
            gdFobj.FolderId__c =  gdLst.FolderId__c;
            gdFobj.Business_License_Application__c = setBLAId[0];
            gdFobj.CompanyName__c = gdLst.CompanyName__c;
            gdFobj.Account__c = blaAccountId;
            googleDriveListToInsert.add(gdFobj);
            
            
        }
        system.debug('googleDriveListToInsert---'+googleDriveListToInsert.size());
        if(googleDriveListToInsert.size()>0){
            Insert googleDriveListToInsert;
        }
        
    }
    public static void sendingFiletoFolder(list<id> BLAId){
        system.debug('Inside the sendingFiletoFolder');
        map<id,ContentVersion> fileandFolderId = new map<id,ContentVersion>();
        map<string,string>mapfileandFolderId = new map<string,string>();
        
        list<id> contentDocumentIds = new list<id>();
        List<OrganicsApplication__c> lstOrgApp =[select Id,name from OrganicsApplication__c where Business_License_Application__c In:BLAId];
        system.debug('---------lstOrgApp-------'+lstOrgApp);
        Set<Id> OrganicsAppId = new Set<Id>();
        List<string> lstFiles = new List<string>();
        
        for (OrganicsApplication__c orgApp : lstOrgApp) {
            OrganicsAppId.add(orgApp.Id);
        }
        system.debug('----OrganicsAppId-------'+OrganicsAppId);
        
        for(ContentDocumentLink  objContDoc : [SELECT ContentDocument.Title FROM ContentDocumentLink WHERE LinkedEntityId IN:OrganicsAppId]){
            
            string fileTitle = objContDoc.ContentDocument.Title;
            integer lastIndex = fileTitle.lastIndexOf(' ');
            fileTitle = fileTitle.substring(0, lastIndex);
            system.debug(fileTitle);
            //lstFiles.add(objContDoc.ContentDocument.Title);
            lstFiles.add(fileTitle);
            contentDocumentIds.add(objContDoc.ContentDocumentId);
            
        }
        
        system.debug('----lstFiles------'+lstFiles);
        for(GoogleDrive_Folder__c GDFId  : [select Id,Name,FolderId__c from GoogleDrive_Folder__c where Business_License_Application__r.Id IN:BLAId and  Name IN: lstFiles]){
            mapfileandFolderId.put(GDFId.Name, GDFId.FolderId__c);
        }
        
        system.debug('mapfileandFolderId'+mapfileandFolderId);
        //ID jobID = System.enqueueJob(new GDriveFileUploadController(contentDocumentIds,mapfileandFolderId));
        
    }
    
}