exec("./BSD_window_Resizer.gui");


BSD_Window.resizeWidth = 1;
BSD_Window.resizeHeight = 1;


if($Pref::BSD::Scale $= "")
	$Pref::BSD::Scale = 0;

if($Pref::BSD::LastResolution $= "")
	$Pref::BSD::LastResolution = "640 480";
$BSD::FirstResize = 0;


function execBrick()
{
	exec("config/client/serverCommandgui/functions/BrickSelecterDialog.cs");
	//bsd_window.extent = "640 480"; BSD_onResize.onResize();
}

function BSD_findCategory(%catName)
{
	%cat = $BSD_nameCategory[%catName];
	if(%cat $= "")
	{
		for(%i = 0; %i < $BSD_numCategories; %i++)
			$BSD_nameCategory[$BSD_category[%i].name] = $BSD_category[%i];

		return $BSD_nameCategory[%catName];
	}
	else return %cat;
}


function BSD_findSubCategory(%catObj, %subCatName)
{
	%cat = %catObj.subCategoryName[%subCatName];
	if(%cat $= "")
	{
		for(%i = 0; %i < %catObj.numSubCategories; %i++)
			%catObj.subCategoryName[%catObj.subCategory[%i].name] = %catObj.subCategory[%i];

		return %catObj.subCategoryName[%subCatName];
	} else return %cat;
}
function BSD_KillBricks()
{
	%numCats = $BSD_numCategories;
	for(%i = 0; %i < %numCats; %i++)
		$BSD_category[%i].delete();

	$BSD_numCategories = 0;
	if(isObject(BSD_InvBox))
		BSD_InvBox.delete();

	if(isObject(BSD_TabBox))
		BSD_TabBox.delete();

	if(isObject(BSD_ScrollBox))
		BSD_ScrollBox.delete();

	for(%i = 0; %i < $BSD_NumInventorySlots; %i++)
	{
		$BSD_InvData[%i] = -1.0;
		$BSD_InvIcon[%i] = -1.0;
	}
	if (isObject(BSD_Group))
		BSD_Group.delete();

	deleteVariables("$BSD_nameCategory*");
	deleteVariables("$BSD_SubCategoryGui*");
}


function BSD_nameUnkowns()
{
	%bsd["GuiDefaultProfile", "3 420", "635 2"] = "BSD_Border_BottomWhite";
	%bsd["GuiDefaultProfile", "3 55",  "634 2"] = "BSD_Border_TopGray";
	%win = nameToID("BSD_Window");
	%c = %win.getCount();
	for(%I = 0; %I < %c; %I++)
	{
		%o = %win.getObject(%I);
		if(%o.getName() !$= "")
			continue;
		%check = %bsd[%o.profile, %o.position, %o.extent];
		if(%check !$= "")
			%o.setName(%check);
	}
}
function BSD_addCategory(%newcat)
{
	for(%i = 0; %i < $BSD_numCategories; %i++)
		if ($BSD_category[%i].name $= %newcat)
			return;

	%category = new ScriptObject(BSD_category)
	{
		name = %newcat;
		numSubCategories = 0;
	};
	$BSD_category[$BSD_numCategories] = %category;
	BSD_Group.add($BSD_category[$BSD_numCategories]);
	$BSD_numCategories++;
}
function BSD_addSubCategory(%cat, %newSubCat)
{
	%catID = -1.0;
    for(%i = 0; %i < $BSD_numCategories; %i++)
	{
		if ($BSD_category[%i].name $= %cat)
			%catID = %i;
	}
	if (%catID == -1.0)
	{
		error("Error: BSD_addSubCategory - category ", %cat, " not found.");
		return;
	}

    for(%i = 0; %i < $BSD_category[%catID].numSubCategories; %i++)
	{
		if ($BSD_category[%catID].subCategory[%i].name $= %newSubCat)
			return $BSD_category[%catID].subCategory[%i];
	}
	$BSD_category[%catID].subCategory[$BSD_category[%catID].numSubCategories] = new ScriptObject(BSD_SubCategory)
	{
		name = %newSubCat;
		numBrickButtons = 0;
	};
	BSD_Group.add($BSD_category[%catID].subCategory[$BSD_category[%catID].numSubCategories]);
	$BSD_category[%catID].numSubCategories = $BSD_category[%catID].numSubCategories + 1;
	return $BSD_category[%catID].subCategory[$BSD_category[%catID].numSubCategories - 1];
}


$tmbi_dragenabled = 0;
function BSD_onResize::onResize(%this, %noResize)
{
	if(!$BSD::FirstResize)
	{
		$BSD::FirstResize = 1;
		%xy = BSD_Window.position;
		BSD_Window.resize(getWord(%xy, 0), getWord(%xy, 1), getWord($Pref::BSD::LastResolution, 0), getWord($Pref::BSD::LastResolution, 1));

		%skip = 1;
	}
    %window = BSD_Window.getID();
    %x = getWord(%window.extent, 0);
    %y = getWord(%window.extent, 1);
	$tmbi_dragenabled = 0;

	$Pref::BSD::LastResolution = %x SPC %y;
	if(isObject(tmbi_resizer))
	{
		%tmbi_pos = tmbi_resizer.position;
		tmbi_resizer.resize(getWord(%tmbi_pos, 0), getWord(%tmbi_pos, 1), getWord(%window.extent, 0) - 3, 9);

		BSD_Window.pushtoback(tmbi_resizer);
	}
	if($Pref::BSD::Scale $= $BSD_LastScale && %window.extent $= %window.lastExtent && !%skip)
		return;

	%topBar = nameToID("BSD_Border_TopGray");
	if(!isObject(%topBar))
	{
		BSD_nameUnkowns();
		%topBar = nameToID("BSD_Border_TopGray");
	}
	if(isObject(%topBar))
		%topBar.resize(getWord(%topBar.position, 0), getWord(%topBar.position, 1), %x, 2);

	%window.lastExtent = %window.extent;
	$BSD_LastScale = $Pref::BSD::Scale;

	%scale = 96 * ($Pref::BSD::Scale + 1);
	%scaleD = %scale + 1;

	%countPerLine = mFloor(((%x - 16) - 18 * 2) / %scale);

	%tmbi = isObject(TMBI_ScrollBox) || ($tmbi::version !$= "");

	if(%tmbi)
	{
		 //            + 59 + 20 + 5; //= 59(box) + 20(space at bottom) + 156(tmbi scale) + 5 offset
    	BSD_ScrollBox.extent = %x-6 SPC %y - (getWord(TMBI_ScrollBox.getExtent(), 1) + 84);
		// %pos_x =    3 + ((getWord(BSD_Window.extent, 0) - 634) / 2);
		// %pos_y = getWord(TMBI_ScrollBox.position, 1);
		// %ext_x = getWord(TMBI_ScrollBox.extent, 0);
		// %ext_y = getWord(TMBI_ScrollBox.extent, 1);
		// TMBI_ScrollBox.resize(%pos_x, %pos_y, %ext_x, %ext_y);
	} else {
		BSD_ScrollBox.extent = %x-6 SPC %y-117;
	}

	BSD_InvBox.position = 3 SPC %y-59;

	%scrollExt = BSD_ScrollBox.extent;

    for(%i = 0; %i < $BSD_numCategories; %i++)
    {
        %cat = $BSD_category[%i];
        %cat.box.extent = %x-6 SPC 6;
        //%cat.scroll.extent = %x-6 SPC %y-117;
		%cat.scroll.extent = %scrollExt;

		%box = %cat.box;
		%sExt = %box.extent;
		%c = %cat.numSubCategories;
		for(%k = 0; %k < %c; %k++)
		{
			%subCat = %cat.subCategory[%k];

			%boxExtent = %box.extent;
			%boxMinExtent = %box.minextent;
			%boxHeight = getWord(%boxExtent, 1) - getWord(%boxMinExtent, 1);
			%subCat.startHeight = %boxHeight;


			$BSD_SubCategoryGui[%cat, %subCat, "BAR"].position = 18 SPC %boxHeight + 15;
			$BSD_SubCategoryGui[%cat, %subCat, "HEAD"].position = 28 SPC %boxHeight - 2;

			%cc = %subCat.numBrickButtons;
			%numRows = mCeil(%cc / %countPerLine);

			%box.extent = %x-6 SPC %boxHeight + 18 + %numRows * %scale + 5;

			for(%J = 0; %J < %cc; %J++)
			{
				%sub_x = (%J % %countPerLine)       * %scaleD + 18;
				%sub_y = ((%J / %countPerLine) | 0) * %scaleD + %boxHeight + 18;

				$BSD_SubCategoryGui[%cat, %subCat, %J, "BG"  ].resize(%sub_x, %sub_y, %scale, %scale);
				$BSD_SubCategoryGui[%cat, %subCat, %J, "Icon"].resize(%sub_x, %sub_y, %scale, %scale);
				$BSD_SubCategoryGui[%cat, %subCat, %J, "ACT" ].resize(%sub_x, %sub_y, %scale, %scale);
				$BSD_SubCategoryGui[%cat, %subCat, %J, "BTN" ].resize(%sub_x, %sub_y, %scale, %scale);


				$BSD_SubCategoryGui[%cat, %subCat, %J, "LAB" ].resize(%sub_x, %sub_y+%scale-18, %scale, 18);
			}
		}

		%box.resize(0, 0, %x - 6, getWord(%box.extent, 1));
    }

    BSD_DoneButton.position = 566 SPC %y - 52;
	BSD_ClearBtn.position   = 5 SPC %y - 69;
	BSD_Window.pushToBack(BSD_DoneButton);
	BSD_Window.pushToBack(BSD_ClearBtn);


	//echo(RESIZE);
	if(!%noResize)
		%this.schedule(33, onResize, %noResize = true);
}

function BSD_LoadBricks()
{
	BSD_Window.minScale = "640 480";
	BSD_KillBricks();

	%group = new SimGroup(BSD_Group){};
	RootGroup.add(%group);
	$BSD_numCategories = 0;
    %brickCount = 0;
	%dbCount = getDataBlockGroupSize(); // getMin(getDataBlockGroupSize(), 10000); // getDataBlockGroupSize() only works correctly as the host / before ghosting

	for(%i = 0; %i < %dbCount; %i++)
	{
		%db = getDataBlock(%i);
		%dbClass = %db.getClassName();
		if (%dbClass $= "fxDTSBrickData")
		{
			%cat = %db.category;
			%subCat = %db.subCategory;
			%uiName = %db.uiName;
			if (%cat !$= "" && %subCat !$= "" && %uiName !$= "")
			{
                %brick[%brickCount] = %db;
                %brickCount++;
				BSD_addCategory(%cat);
				%subCatObj = BSD_addSubCategory(%cat, %subCat);
				%subCatObj.numBricks = %subCatObj.numBricks + 1.0;
			}
		}
	}

	%w_x = getWord(BSD_Window.extent, 0);
	%w_y = getWord(BSD_Window.extent, 1);

	%newScrollBox = new GuiControl() { };
	BSD_Window.add(%newScrollBox);
	%x = 3;
	%y = 57;
	%w = %w_x - 6;
	%h = %w_y - 117;
	%newScrollBox.resize(%x, %y, %w, %h);
	%newScrollBox.setName("BSD_ScrollBox");

	%newTabBox = new GuiControl() { };
	BSD_Window.add(%newTabBox);
	%x = 3;
	%y = 30;
	%w = 6340;
	%h = 25;
	%newTabBox.resize(%x, %y, %w, %h);
	%newTabBox.setName("BSD_TabBox");

	for(%i = 0; %i < $BSD_numCategories; %i++)
	{
		%newTab = new GuiBitmapButtonCtrl() { };
		BSD_TabBox.add(%newTab);
		%newTab.setProfile(BlockButtonProfile);
		%x = %i * 80.0;
		%y = 0;
		%w = 80;
		%h = 25;
		%newTab.resize(%x, %y, %w, %h);
		%newTab.setText($BSD_category[%i].name);
		%newTab.setBitmap("base/client/ui/tab1");
		%newTab.command = "BSD_ShowTab(" @ %i @ ");";

		%newScroll = new GuiScrollCtrl() { };
		BSD_ScrollBox.add(%newScroll);
		%newScroll.rowHeight = 64;
		%newScroll.hScrollBar = "alwaysOff";
		%newScroll.vScrollBar = "alwaysOn";
		%newScroll.setProfile(BSDScrollProfile);
		%newScroll.defaultLineHeight = 32;

        //scroll box
		%x = 0;
		%y = 0;
		%w = %w_x - 6;
		%h = %w_y - 117;
		%newScroll.resize(%x, %y, %w, %h);

		%newBox = new GuiControl() { };
		%newScroll.add(%newBox);
		%newBox.setProfile(ColorScrollProfile);
		%x = 0;
		%y = 0;
		%w = 0;
		%h = 0;
		%newBox.resize(%x, %y, %w, %h);

		$BSD_category[%i].tab = %newTab;
		$BSD_category[%i].Scroll = %newScroll;
		$BSD_category[%i].box = %newBox;
		BSD_createSubHeadings($BSD_category[%i]);
	}
    for(%i = 0; %i < %brickCount; %i++)
		BSD_CreateBrickButton(%brick[%I]);

	BSD_CreateInventoryButtons();
	BSD_InvBox.position = 3 SPC %w_y-59;
	BSD_Window.pushToBack(BSD_ClearBtn);
	BSD_ShowTab(0);

    if(!isObject(BSD_onResize))
        new GuiMLTextCtrl(BSD_onResize) { profile = GuiMLTextProfile; };
    BSD_Window.add(BSD_onResize);
    BSD_Window.resize = BSD_onResize;

	$BSD::FirstResize = 0;
}


function BSD_CreateBrickButton(%data)
{
	%catName = %data.category;
	%subCatName = %data.subCategory;
	%catObj = BSD_findCategory(%catName);
	%subCatObj = BSD_findSubCategory(%catObj, %subCatName);
	if (%catObj == 0.0 || %subCatObj == 0.0)
	{
		error("ERROR: BSD_CreateBrickButton - Couldnt find category objects");
		return;
	}
	%brickName = %data.uiName;
	%brickIcon = %data.iconName;
	if (%brickName $= "")
		%brickName = "No Name";

	if(!isFile(%brickIcon @ ".png"))
		%brickIcon = "base/client/ui/brickIcons/unknown";

    %extent = %catObj.box.extent;

	%offsetX = 18; //18;
	%offsetY = 18; //18;

	%scale = 96 * ($Pref::BSD::Scale+1);
	%countPerLine = mFloor((firstWord(BSD_ScrollBox.extent - 16) - %offsetX * 2) / %scale);


    %scaleD = %scale+1;

	%box = %catObj.box;
	%count = %subCatObj.numBrickButtons;

	%x = (%count % %countPerLine) * %scaleD + %offsetX;
	%y = mFloor(%count / %countPerLine) * %scaleD + %subCatObj.startHeight + %offsetY;



	%subCatObj.numBrickButtons++;

	%newIconBG = new GuiBitmapCtrl(){ };
	%box.add(%newIconBG);
	%newIconBG.resize(%x, %y, %scale, %scale);
	%newIconBG.keepCached = 1;
	%newIconBG.setBitmap("base/client/ui/brickicons/brickiconbg");
	%newIconBG.setProfile(BlockDefaultProfile);
	//$BSD_gui

	%newIcon = new GuiBitmapCtrl(){ };
	%box.add(%newIcon);
	%newIcon.resize(%x, %y, %scale, %scale);
	%newIcon.keepCached = 1;
	%newIcon.setBitmap(%brickIcon);
	%newIcon.setProfile(BlockDefaultProfile);

	%newActive = new GuiBitmapCtrl(){ };
	%box.add(%newActive);
	%newActive.resize(%x, %y, %scale, %scale);
	%newActive.keepCached = 1;
	%newActive.setBitmap("base/client/ui/brickicons/brickIconActive");
	%newActive.setProfile(BlockDefaultProfile);
	%newActive.setVisible(0);
	$BSD_activeBitmap[%data] = %newActive;

	%newIconButton = new GuiBitmapButtonCtrl(){};
	%box.add(%newIconButton);
	%newIconButton.resize(%x, %y, %scale, %scale);
	%newIconButton.setBitmap("base/client/ui/brickicons/brickIconBtn");
	%newIconButton.setProfile(BlockButtonProfile);
	%newIconButton.setText(" ");
	%newIconButton.command = "BSD_ClickIcon(" @ %data @ ");";
	%newIconButton.altCommand = "BSD_RightClickIcon(" @ %data @ ");";

	%newLabel = new GuiTextCtrl(){};
	%box.add(%newLabel);
	%w = %scale;
	%h = 18;
	%x = %x;
	%y = %y + %scale - %h;
	%newLabel.resize(%x, %y, %w, %h);
	%newLabel.setProfile(HUDBSDNameProfile);
	%newLabel.setText(%brickName);

	$BSD_SubCategoryGui[%catObj, %subCatObj, %count, "BG"]   = %newIconBG;
	$BSD_SubCategoryGui[%catObj, %subCatObj, %count, "Icon"] = %newIcon;
	$BSD_SubCategoryGui[%catObj, %subCatObj, %count, "ACT"]  = %newActive;
	$BSD_SubCategoryGui[%catObj, %subCatObj, %count, "BTN"]  = %newIconButton;
	$BSD_SubCategoryGui[%catObj, %subCatObj, %count, "LAB"]  = %newLabel;
}

function BSD_createSubHeadings(%cat)
{
	%scale = 96 * ($Pref::BSD::Scale+1);
	%countPerLine = mFloor((firstWord(BSD_ScrollBox.extent) - 18*2) / %scale);

	%box = %cat.box;
    for(%i = 0; %i < %cat.numSubCategories; %i++)
	{
		%subCatObj = %cat.subCategory[%i];

		%boxExtent = %box.getExtent();
		%boxMinExtent = %box.getMinExtent();
		%boxHeight = getWord(%boxExtent, 1) - getWord(%boxMinExtent, 1);
		%subCatObj.startHeight = %boxHeight;
		%newBar = new GuiBitmapCtrl() { };
		%box.add(%newBar);
		%newBar.keepCached = 1;
		%x = 0.0 + 18.0;
		%y = %subCatObj.startHeight + 15.0;
		%w = getWord(getRes(), 0); //not sure about performance of large guis
		%h = 1;
		%newBar.resize(%x, %y, %w, %h);

		%newHeading = new GuiTextCtrl() { };
		%box.add(%newHeading);
		%newHeading.setProfile(BrickListSubCategoryProfile);
		%newHeading.setText(%subCatObj.name);
		%x = 0.0 + 18.0 + 10.0;
		%y = %subCatObj.startHeight - 2.0;
		%w = 1080;
		%h = 18;
		%newHeading.resize(%x, %y, %w, %h);


		%numRows = mCeil(%subCatObj.numBricks / %countPerLine);

		%x = 0;
		%y = 0;
		%w = firstWord(BSD_ScrollBox.extent);
		%h = %boxHeight + 18.0 + %numRows * %scale + 5.0;
		%box.resize(%x, %y, %w, %h);

		$BSD_SubCategoryGui[%cat, %subCatObj, "BAR"] = %newBar;
		$BSD_SubCategoryGui[%cat, %subCatObj, "HEAD"] = %newHeading;
	}
}

//check for TMBI
if(!isPackage(tmbi))
	return;

package tmbi
{
	function brickselectordlg::onwake(%this, %idk)
	{
		//nothing yet
		parent::onwake(%this, %idk);
		tmbi_firsttimeinit();

		if(getWord(BSD_Window.getExtent(), 1) > getWord(playgui.getExtent(), 1))
		{
			tmbi_debug("oh dear, your pitifully small screen can't handle the awesome.");
			BSD_Window.resize(getWord(BSD_Window.getPosition(),0), 0, 640, getWord(playgui.getExtent(), 1));
		}

		if($tmbi_closepos !$= "")
		{
			tmbi_Debug("Last close position at: "@ $tmbi_closepos);
			//BSD_Window.resize(getWord($tmbi_closepos, 0), getWord($tmbi_closepos, 1), 640, getWord(BSD_Window.getExtent(), 1));
		}

		if(getWord(BSD_Window.getPosition(), 1) < 0)
		{
			tmbi_debug("repositioning ...");
			BSD_Window.resize(getWord(BSD_Window.getPosition(),0), 0, 640, getWord(BSD_Window.getExtent(), 1));
		}
		TMBI_MainInventory.setvisible(0);
		//BSD_Window.pushtoback(tmbi_resizer); //this keeps moving around for some reason
		commandtoserver('bsd');
	}
};
