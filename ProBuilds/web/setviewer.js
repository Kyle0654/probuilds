
var setmanifest = undefined;
var champions = undefined;
var items = undefined;
var spells = undefined;

var setsdiv = undefined;
var setviewerdiv = undefined;
var helptextdiv = undefined;
var settextarea = undefined;
var setfilename = undefined;
var setdownload = undefined;
var setstatsdiv = undefined;
var set = undefined;

var loadinghref = undefined;

var currenthash = undefined;

var imguri = 'http://ddragon.leagueoflegends.com/cdn/{version}/img/{group}/{filename}'

function getimg(version, group, filename) {
    return imguri.replace('{version}', version).replace('{group}', group).replace('{filename}', filename);
}

function getitem(id) {
    return $.extend(true, items.basic, items.data[id]);
}

function getspell(name) {
    return spells.data[name];
}

function getchampion(key) {
    return champions.data[key];
}

function getchampionbyid(id) {
    var key = champions.keys[id];
    return getchampion(key);
}

// TODO: use sprites
function getitemimg(id) {
    var item = getitem(id);
    return '<img class="item" alt="' + item.name + '" title="' + item.name + '" src="' + getimg(items.version, item.image.group, item.image.full) + '" />';
}

function getspellimg(name) {
    var spell = getspell(name);
    return '<img class="spell" data-spell="' + spell.key + '" alt="' + spell.name + '" title="' + spell.name + '" src="' + getimg(spells.version, spell.image.group, spell.image.full) + '" />';
}

function getchampionimg(key) {
    var champion = getchampion(key);
    return '<img class="champion" alt="' + champion.name + '" title="' + champion.name + '" src="' + getimg(champions.version, champion.image.group, champion.image.full) + '" />';
}

function getSetKey(set) {
    return getchampionbyid(set.ChampionId).name + '_'
        + set.Lane + '_'
        + (set.HasSmite ? "Smite" : "NoSmite");
}

function gethash() {
    if (!window.location.hash) {
        return undefined;
    }

    return window.location.hash.substring(1);
}

function handlehash() {
    var newhash = gethash();

    if (currenthash == newhash)
        return;

    currenthash = newhash;

    if (currenthash == undefined) {
        setviewerdiv.empty();

        if(setstatsdiv != undefined){
            setstatsdiv.empty();
        }

        settextarea.val('');
        setdownload.empty();
        setfilename.text('');
        setdownload.attr('href', undefined);

        //Clear selection
        $('a.set.link').removeClass('selected');

        // Show help
        helptextdiv.show();
        setviewerdiv.hide();
    } else {
        var a = $('a.set.link').filter(function () { return $(this).attr('data-name') == currenthash; }).first();
        if (a != undefined) {
            loadset(currenthash, a.attr('href'));
        }
    }
}

$(window).on('hashchange', handlehash);

$(document).ready(function () {
    //Add champion search functionality
    $("#setssearch").keyup(function () {
        //Get the filter value
        var filter = $(this).val(),
            count = 0;

        var searchfilter = new RegExp(filter, "i");

        //Loop through each set and show or hide it based on our search
        $("#sets a").each(function () {
            var name = $(this).attr('data-name');
            var text = $(this).text();
            var length = name.length > 0;

            if (length && name.search(searchfilter) < 0 && text.search(searchfilter) < 0) {
                $(this).hide();
            } else {
                $(this).show();
                count++;
            }
        });
    });

    $('#help').click(function () {
        window.location.hash = '';
        handlehash();
    });
    $('#sitetitle').click(function () {
        window.location = window.location.href.split('#')[0];
    });
});

//Calculate the amount of cold an array of items cost (doesn't factor in items that build into others)
function calculateGold(items) {
    var cost = 0;
    $.each(items, function (i, item) {
        var itemInfo = getitem(item.id);
        cost += itemInfo.gold.total;
    });

    return cost;
}

function getblockhtml(block) {
    var blockhtml = '';

    //Get the cost of the items in this block
    var cost = calculateGold(block.items);

    var blocktitle = block.type + ' (' + cost + ' Gold)';

    var data = '';
    if (block.showIfSummonerSpell) {
        data += ' data-show-spells="' + block.showIfSummonerSpell + '"';
    }
    if (block.hideIfSummonerSpell) {
        if (data != '') {
            data += ' ';
        }

        data += 'data-hide-spells="' + block.hideIfSummonerSpell + '"';
    }

    blockhtml += '<span class="block title">' + blocktitle + '</span>';

    blockhtml += '<div class="block itemcontainer">';
    $.each(block.items, function (i, item) {
        if (i != 0) {
            if (block.recMath == true) {
                blockhtml += (i < block.items.length - 1) ? ' + ' : ' -&gt; ';
            } else {
                blockhtml += ' ';
            }
        }

        blockhtml += '<div class="block item">' + getitemimg(item.id) + '<br/>' + item.count + '<br/>' + (item.hasOwnProperty("percentage") ? ((item.percentage * 100).toFixed(0) + '%') : '') + '</div>';
    });

    blockhtml += '</div>';

    return '<div class="block container" ' + data + '>' + blockhtml + '</div>';
}

//Add item stats to champion stats
function addItemStats(championStats, itemStats) {

    championStats.armor += itemStats.FlatArmorMod;
    championStats.attackdamage += itemStats.FlatPhysicalDamageMod;
    championStats.attackspeedoffset += itemStats.FlatAttackSpeedMod;
    championStats.crit += itemStats.FlatCritChanceMod;
    championStats.hp += itemStats.FlatHPPoolMod;
    championStats.hpregen += itemStats.FlatHPRegenMod;
    championStats.movespeed += itemStats.FlatMovementSpeedMod;
    championStats.mp += itemStats.FlatMPPoolMod;
    championStats.mpregen += itemStats.FlatMPRegenMod;
    championStats.spellblock += itemStats.FlatSpellBlockMod;

    return championStats;
}

//Add item to our item stats
function combineItemStats(itemStats, item) {
    var newItemStats = getitem(item.id).stats;

    if (itemStats == undefined) {
        //Assign the stats and properties
        itemStats = $.extend(true, {}, newItemStats);

        //Make sure our count is applied to the stats
        $.each(itemStats, function (key, value) {
            if (value > 0) {
                itemStats[key] = value * item.count;
            }
        });
    } else {

        //Add all the stats we can
        $.each(newItemStats, function (key, value) {
            //Only care about positive values
            if (value > 0) {

                //If we don't have the property add it else add the value
                if (itemStats.hasOwnProperty(key)) {
                    itemStats[key] += value * item.count;
                } else {
                    itemStats[key] = value * item.count;
                }
            }
        });
    }

    return itemStats;
}

//Remove properties that are zero
function cleanItemStats(itemStats) {
    $.each(itemStats, function (key, value) {
        if (value <= 0) {
            delete itemStats[key];
        }
    });

    return itemStats;
}

function getStatshtml(block, championKey) {
    var blockhtml = '';

    var blocktitle = block.type;

    var data = '';
    if (block.showIfSummonerSpell) {
        data += ' data-show-spells="' + block.showIfSummonerSpell + '"';
    }
    if (block.hideIfSummonerSpell) {
        if (data != '') {
            data += ' ';
        }

        data += 'data-hide-spells="' + block.hideIfSummonerSpell + '"';
    }

    blockhtml += '<span class="block title">' + blocktitle + '</span>';

    blockhtml += '<div class="block itemcontainer">';

    //Create stat variables
    //var championStats = $.extend(true, {}, getchampion(championKey).stats);
    var itemStats = undefined;

    //Add up all the stats
    $.each(block.items, function (i, item) {
        itemStats = combineItemStats(itemStats, item);
    });

    //Clean empty values
    itemStats = cleanItemStats(itemStats);

    //Add all the html for the stats
    $.each(itemStats, function (key, value) {
        //Get the important part
        var statText = key.replace("Flat", "").replace("Mod", "").replace("Pool", "");

        //Put some spaces in
        statText = statText.replace(/([A-Z][a-z]+)/g, ' $1');

        //Check for percent
        var isPercent = (statText.indexOf("Percent") > -1);
        var valueText = value;
        if (isPercent) {
            statText = statText.replace("Percent", "");
            valueText = valueText * 100;
            valueText = Math.round(valueText * 100) / 100;
            valueText = valueText + '%';
        }


        //Get the html
        blockhtml += '<div class="block stats">' + statText + ': <font color="#2E64FE">' + valueText + '</font></div>';
    });

    blockhtml += '</div>';

    return (Object.keys(itemStats).length > 0 ? '<div class="block container" ' + data + '>' + blockhtml + '</div>' : '');
}

//Get html text for a tab
//arg uniqueName A unique name used for the id of this tab
//arg title Title text for the tab
//arg contentHTML (optional) html text to go within this tab, if not supplied you need to insert it later with javascript using $("#source").appendTo("#tab-<uniqueName>");
function getTabHTML(uniqueName, title, contentHTML){
    var tab = '<section id="tab-' + uniqueName + '">';
    tab += '<h2><a href="#tab-' + uniqueName + '">' + title + '</a></h2>';
    if(contentHTML != undefined && contentHTML != '') {
        tab += contentHTML;
    }
    tab += '</section>';

    return tab;
}

//Add a tabs panel on the right inserting the download content into a tab
//and adding a stats tab
function buildTabsPane() {

    //Create the tabs
    var tabsHTML = '<article class="tabs">';
    tabsHTML += getTabHTML("download", "Download");
    tabsHTML += getTabHTML("stats", "Stats", '<div class="rightcolumn" id="setstats"></div>');
    tabsHTML += '</article>';

    //Insert the tabs under root and below the content we will insert into a tab
    $("#settext").after(tabsHTML);
    $("#settext").appendTo("#tab-download");

    //Set our variable
    setstatsdiv = $('#setstats');
}

function spellClick() {
    $(this).toggleClass('active');

    updateSpellBlockVisibility.call($(this));
}

function updateSpellBlockVisibility() {
    var key = $(this).attr('data-spell');
    var active = $(this).hasClass('active');

    setviewerdiv.find(".block.container[data-show-spells='" + key + "']").toggle(active);
    setviewerdiv.find(".block.container[data-hide-spells='" + key + "']").toggle(!active);

    if (setstatsdiv != undefined) {
        setstatsdiv.find(".block.container[data-show-spells='" + key + "']").toggle(active);
        setstatsdiv.find(".block.container[data-hide-spells='" + key + "']").toggle(!active);
    }
}

function getspellshtml(showspells) {
    var spellshtml = '';

    $.each(showspells, function (name, active) {
        if (active) {
            var spellimg = getspellimg(name);
            spellshtml += spellimg;
        }
    });

    return spellshtml;
}

function loadset(sethash, href) {
    loadinghref = href;
    $.getJSON(href, function (data) {
        if (loadinghref != href) {
            return;
        }

        // hide help
        helptextdiv.hide();
        setviewerdiv.show();

        $('a.set.link').removeClass('selected');

        var setlink = $("a.set.link[href='" + loadinghref + "']");
        setlink.addClass('selected');

        var champkey = setlink.attr('data-champ-key');
        var champion = getchampion(champkey);
        var champimg = getchampionimg(champkey);

        setviewerdiv.empty();
        if(setstatsdiv != undefined){
            setstatsdiv.empty();
        }

        set = data;

        // Create set display
        var title = '<span class="set title">' + champimg + ' ' + champion.name + ' ' + set.title + '</span>';
        setviewerdiv.append(title);

        //Add the spells buttons
        var showspells = {};
        $.each(data.blocks, function (i, block) {
            if (block.showIfSummonerSpell) {
                if(!showspells.hasOwnProperty(block.showIfSummonerSpell)) {
                    showspells[block.showIfSummonerSpell] = true;
                }
            }
        });
        var spellshtml = getspellshtml(showspells);
        setviewerdiv.append(spellshtml);
        setviewerdiv.find('img.spell').click(spellClick);

        //Add all the blocks
        $.each(data.blocks, function (i, block) {
            var blockhtml = getblockhtml(block);
            setviewerdiv.append(blockhtml);
        });

        //Add the stats
        if(setstatsdiv != undefined){
            var statsDisclaimer = '<p>All stats are simply a sum of all the items stats in this block.</p>';
            setstatsdiv.append(statsDisclaimer);
            var championKey = href.split("/")[1];
            $.each(data.blocks, function (i, block) {
                var blockhtml = getStatshtml(block, championKey);
                setstatsdiv.append(blockhtml);
            });
        }

        //Update the block visibility based on spells
        setviewerdiv.find('img.spell').each(function () {
            updateSpellBlockVisibility.call($(this));
        });

        // Set text area display
        var jsonstr = JSON.stringify(set, null, 2);
        settextarea.val(jsonstr);

        // Set filename
        setfilename.text(href.substring(setmanifest.root.length));

        // Set download link
        setdownload.attr('href', href);

        // Set hash
        if (gethash() != sethash) {
            window.location.hash = sethash;
            currenthash = sethash;
        }
    });
}

function initializesets() {
    setsdiv.empty();
    setviewerdiv.empty();
    if(setstatsdiv != undefined){
        setstatsdiv.empty();
    }

    // Create links
    var allsets = $.map(setmanifest.sets, function (value, index) {
        return {
            'championkey': index,
            'sets': $.map(value, function (setdata, key) {
                return {
                    'key': setdata.Key,
                    'file': setdata.File,
                    'title': setdata.Title
                }
            })
        };
    });

    allsets.sort(function (a, b) { return getchampion(a.championkey).name.localeCompare(getchampion(b.championkey).name) });

    $.each(allsets, function (i, setsdata) {
        var champkey = setsdata.championkey;
        var champion = getchampion(champkey);
        var champimg = getchampionimg(champkey);
        $.each(setsdata.sets, function (j, setdata) {
            var setkey = getSetKey(setdata.key);
            var seturi = setdata.file;
            var settitle = setdata.title;

            setsdiv.append('<a data-name="' + setkey + '" data-champ-key="' + champkey + '" class="set link" href="' + seturi + '" download>'
                + champimg + ' '
                + '<span class="set champion">' + champion.name + '</span><br/>'
                + '<span class="set title">' + settitle + '</span>'
                + '</a>');
        });
    });

    setsdiv.find('a').click(function () {
        var sethash = $(this).attr('data-name');
        var href = $(this).attr('href');
        loadset(sethash, href);
        return false;
    });
}

// Thin wrapper so we can change init sequence easily
function init() {
    initChampions();
}

function initChampions() {
    $.getJSON('champions.json', function (data) {
        champions = data;
    }).done(initItems);
}

function initItems() {
    $.getJSON('items.json', function (data) {
        items = data;
    }).done(initSpells);
}

function initSpells() {
    $.getJSON('summonerspells.json', function (data) {
        spells = data;
    }).done(initManifest);
}

function initManifest() {
    $.getJSON('setmanifest.json', function (data) {
        setmanifest = data;
        initializesets();
        handlehash();
    });
}

$(function () {
    searchdiv = $('#search');
    setsdiv = $('#sets');
    setviewerdiv = $('#setviewer');
    helptextdiv = $('#helptext');
    settextarea = $('#settextarea');
    setfilename = $('#setfilename');
    setdownload = $('#setdownload');
    //buildTabsPane();
    init();
});