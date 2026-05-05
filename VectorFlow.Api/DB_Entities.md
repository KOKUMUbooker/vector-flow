## Entity Relationships
### **1.AppUser**
 - Has many : 
	- RefreshTokens
	- Issues
	- Comments
	- Invitations

### **2.RefreshToken**
 - Has one : 
	- User

### **3. Workspace**
 - Has one : 
	- Owner
 - Has many : 
	- WorkspaceMembers (`Members`)
	- Projects
	- Invitations 

### **4. WorkSpaceMember** ***(Joint table between Workspace and AppUser)***
 - Has one : 
	- Workspace
	- AppUser (`User`)

### **5. Project**
 - Has one : 
	- Workspace
 - Has many :
	- Issues
	- Labels

### **6. Issue**
 - Has one : 
	- User (`Assignee`)
	- User (`Reporter`)
	- Project 
 - Has many : 
	- Comments
	- ActivityLogs
	- IssueLabels 

### **7. Label**
 - Has one :
	- Project
 - Has many :
	- IssueLabels

### **8. IssueLabel** ***(Joint table between Issue and Label)***
 - Has one :
	- Issue
	- Label

### **9. Comment**
 - Has one : 
	- Issue
	- AppUser (`Author`)

### **10. Invitation**
 - Has one : 
	- Workspace
	- AppUser (`InvitedBy`) 

### **11. ActivityLog**
 - Has one : 
	- Issue
	- AppUser (`Actor`)
