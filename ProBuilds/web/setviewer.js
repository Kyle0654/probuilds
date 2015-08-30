
var setmanifest = undefined;
var champions = undefined;
var items = undefined;

var setsdiv = undefined;
var setviewerdiv = undefined;
var settextarea = undefined;
var setfilename = undefined;
var setdownload = undefined;
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
    if (window.location.hash == undefined) {
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
        settextarea.val();
        setdownload.empty();
        setdownload.attr('href', undefined);
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
    if (block.showIfSummonerSpell) {
        blocktitle += ' spell(' + block.showIfSummonerSpell + ')';
    }
    if (block.hideIfSummonerSpell) {
        blocktitle += ' nospell(' + block.hideIfSummonerSpell + ')';
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

    return '<div class="block container">' + blockhtml + '</div>';
}

function loadset(sethash, href) {
    loadinghref = href;
    $.getJSON(href, function (data) {
        if (loadinghref != href) {
            return;
        }

        setviewerdiv.empty();

        set = data;

        // Create set display
        var title = '<span class="set title">' + set.title + '</span>';
        setviewerdiv.append(title);

        $.each(data.blocks, function (i, block) {
            var blockhtml = getblockhtml(block);
            setviewerdiv.append(blockhtml);
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

            setsdiv.append('<a data-name="' + setkey + '" class="set link" href="' + seturi + '" download>'
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
    settextarea = $('#settextarea');
    setfilename = $('#setfilename');
    setdownload = $('#setdownload');
    init();
});