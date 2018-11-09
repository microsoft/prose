var gulp = require('gulp');
var changed = require('gulp-changed');
var concat = require('gulp-concat');
var rename = require('gulp-rename');
var uglify = require('gulp-uglify');
var cleanCSS = require('gulp-clean-css');
var ts = require('gulp-typescript');
var sourcemaps = require('gulp-sourcemaps');
var del = require('del');

gulp.task('mathjax', function() {
    var SRC = 'node_modules/mathjax/**',
        DEST = 'static/mathjax';
    
    return gulp.src(SRC)
        .pipe(gulp.dest(DEST));
});

gulp.task('ts', function () {
    var tsProject = ts.createProject('tsconfig.json');
    return tsProject.src()
        .pipe(sourcemaps.init({loadMaps: true}))
        .pipe(tsProject())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('js'));
});

gulp.task('scripts', gulp.series(gulp.parallel('ts', 'mathjax'), function () {
    var SRC = [
        'node_modules/clipboard/dist/clipboard.min.js',
        'node_modules/jquery/dist/jquery.min.js',
        'node_modules/magnific-popup/dist/jquery.magnific-popup.min.js',
        'js/**/*.js'];
    var DEST = 'static';

    return gulp.src(SRC)
        .pipe(changed(DEST))
        .pipe(sourcemaps.init({loadMaps: true}))
        .pipe(concat('scripts.js'))
        .pipe(gulp.dest(DEST))
        .pipe(rename('scripts.min.js'))
        .pipe(uglify())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest(DEST));
}));

gulp.task('fonts', function() {
    var SRC = 'node_modules/font-awesome/fonts/*',
        DEST = 'fonts';
    
    return gulp.src(SRC)
        .pipe(gulp.dest(DEST));
});

gulp.task('css', gulp.series('fonts', function () {
    var SRC = [
        'node_modules/ace-css/css/ace.min.css',
        'node_modules/font-awesome/css/font-awesome.min.css',
        'node_modules/magnific-popup/dist/magnific-popup.css',
        'node_modules/normalize.css/normalize.css',
        'node_modules/purecss/build/pure-min.css',
        'node_modules/purecss/build/grids-responsive-min.css',
        'css/vendor/*.css',
        'css/main.css'];

    return gulp.src(SRC)
        .pipe(sourcemaps.init())
        .pipe(concat('styles.css'))
        .pipe(gulp.dest('static'))
        .pipe(cleanCSS({compatibility: 'ie8'}))
        .pipe(rename('styles.min.css'))
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('static'));
}));

gulp.task('clean-output', function() {
    return del([
        'static/**', 
        'fonts/**']);
});

gulp.task('default', gulp.series('clean-output', gulp.parallel('css', 'scripts')));
